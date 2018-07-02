using Open.IO;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Net.Http
{
    public class StreamedContent : HttpContent
    {
        private CancellationToken _cancellationToken;
        private Stream _fileStream;
        private IProgress<StreamProgress> _progress;

        private class ContentStream : StreamWrapper
        {
            private IProgress<StreamProgress> _progress;
            private long _position = 0;
            public ContentStream(Stream stream, IProgress<StreamProgress> progress)
                : base(stream)
            {
                _progress = progress;
            }
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var readBytes = await base.ReadAsync(buffer, offset, count, cancellationToken);
                _position += readBytes;
                _progress?.Report(new StreamProgress(_position, Length));
                return readBytes;
            }
        }
        public StreamedContent(Stream fileStream, IProgress<StreamProgress> progress, CancellationToken cancellationToken)
        {
            _fileStream = fileStream;
            _progress = progress;
            _cancellationToken = cancellationToken;
        }
        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(new ContentStream(_fileStream, _progress));
        }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await _fileStream.CopyToAsync(stream, progress: _progress, flush: true, cancellationToken: _cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _fileStream.Length;
            return true;
        }
    }
}
