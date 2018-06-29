using Open.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Net.Http
{
    public static class HttpClientEx
    {
        public static async Task<T> TryReadJsonAsync<T>(this HttpContent content)
        {
            try
            {
                return await ReadJsonAsync<T>(content);
            }
            catch
            {
                return default(T);
            }
        }

        public static async Task<T> ReadJsonAsync<T>(this HttpContent content)
        {
            Stream stream = null;
            try
            {
                stream = await content.ReadAsStreamAsync();
                var result = stream.DeserializeJson<T>();
                return result;
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        private const int CHUNKSIZE = 1024;

        public static async Task<byte[]> ReadAsByteArrayAsync(this HttpContent content, CancellationToken cancellationToken, IProgress<BytesProgress> progress)
        {
            var length = content.Headers.ContentLength;
            using (var memoryStream = new MemoryStream())
            {
                var stream = await content.ReadAsStreamAsync();
                var buffer = new byte[CHUNKSIZE];
                int readBytes = 0;
                long bytesReceived = 0;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    readBytes = await stream.ReadAsync(buffer, 0, CHUNKSIZE);
                    if (readBytes > 0)
                    {
                        bytesReceived += readBytes;
                        await memoryStream.WriteAsync(buffer, 0, readBytes);
                        progress?.Report(new BytesProgress(bytesReceived, length.Value));
                    }
                }
                while (readBytes > 0);
                return memoryStream.ToArray();
            }
        }

        public static void SetEmptyContent(this HttpRequestMessage request)
        {
            request.Content = GetEmptyContent();
        }
        public static HttpContent GetEmptyContent()
        {
            return new ByteArrayContent(new byte[0]);
        }
    }
}
