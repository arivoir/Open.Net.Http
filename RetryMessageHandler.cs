using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Net.Http
{
    public class RetryMessageHandler : DelegatingHandler
    {
        private int _maxRetries;

        public RetryMessageHandler(IHttpMessageHandlerFactory messageHandlerFactory = null, int maxRetries = 5)
            : base()
        {
            InnerHandler = (messageHandlerFactory ?? HttpMessageHandlerFactory.Default).GetHttpMessageHandler();
            _maxRetries = maxRetries;
        }

        public RetryMessageHandler(HttpMessageHandler innerHandler, int maxRetries = 5)
            : base(innerHandler)
        {
            if (innerHandler == null)
                throw new ArgumentNullException(nameof(innerHandler));
            _maxRetries = maxRetries;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            bool retry; int retries = 0;
            do
            {
                retry = false;
                //Exception e = null;
                try
                {
                    response = await base.SendAsync(request, cancellationToken);
                }
                catch /* (Exception exc)*/
                {
                    throw;
                    //e = exc;
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (retries < _maxRetries)
                {
                    var after = await ShouldRetry(response, retries, cancellationToken);
                    if (after.HasValue)
                    {
                        retry = true;
                        retries++;
                        await Task.Delay(after.Value, cancellationToken);
                        request = await CopyRequestMessage(request);
                    }
                }
                //else
                //{
                //    if (e != null)
                //        throw e;
                //}
            }
            while (retry && retries < _maxRetries);
            return response;
        }

        protected virtual Task<TimeSpan?> ShouldRetry(HttpResponseMessage response, int retries, CancellationToken cancellationToken)
        {
            if (response.Headers.RetryAfter != null)
            {
                return Task.FromResult<TimeSpan?>(response.Headers.RetryAfter.Delta);
            }
            return Task.FromResult<TimeSpan?>(null);
        }

        protected virtual async Task<HttpRequestMessage> CopyRequestMessage(HttpRequestMessage request)
        {
            HttpRequestMessage copy = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                bool result = copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (!result)
                {
                    throw new Exception("Unable to copy headers.");
                }
            }

            foreach (var header in request.Properties)
            {
                copy.Properties.Add(header.Key, header.Value);
            }
            try
            {
                if (request.Content != null)
                    copy.Content = await CopyContent(request.Content);
            }
            catch { }
            return copy;
        }

        private static async Task<HttpContent> CopyContent(HttpContent content)
        {
            var multipartContent = content as MultipartFormDataContent;
            if (multipartContent != null)
            {
                var newContent = new MultipartFormDataContent();
                foreach (var part in multipartContent)
                {
                    var name = part.Headers.ContentDisposition.Name;
                    var fileName = part.Headers.ContentDisposition.FileName;
                    newContent.Add(await CopyContent(part), name, fileName);
                }
                return newContent;
            }
            var streamContent = content as StreamContent;
            if (streamContent != null)
            {
                var stream = await streamContent.ReadAsStreamAsync() as Stream;
                stream.Seek(0, SeekOrigin.Begin);
                return new StreamContent(stream);
            }
            return content;
        }
    }
}
