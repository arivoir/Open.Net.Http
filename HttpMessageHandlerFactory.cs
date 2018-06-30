using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Open.Net.Http
{
    public interface IHttpMessageHandlerFactory
    {
        HttpMessageHandler GetHttpMessageHandler(ICredentials credentials = null, bool allowAutoRedirect = true, bool needsGZipDecompression = false, bool ignoreCertErrors = false);
    }

    public class HttpMessageHandlerFactory : IHttpMessageHandlerFactory
    {
        public static HttpMessageHandlerFactory Default { get; private set; } = new HttpMessageHandlerFactory();

        public HttpMessageHandler GetHttpMessageHandler(ICredentials credentials = null, bool allowAutoRedirect = true, bool needsGZipDecompression = false, bool ignoreCertErrors = false)
        {
            var messageHandler = new HttpClientHandler();
            if (credentials != null)
                messageHandler.Credentials = credentials;
            messageHandler.AllowAutoRedirect = allowAutoRedirect;
            if (needsGZipDecompression)
                messageHandler.AutomaticDecompression = DecompressionMethods.GZip;
            return messageHandler;
        }
    }
}
