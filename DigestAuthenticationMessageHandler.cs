using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Net.Http
{
    public class DigestAuthenticationMessageHandler : RetryMessageHandler
    {
        private DigestAuthentication _authentication;
        private string _parameter;
        private string _method;
        private Uri _uri;
        private int _nc = 0;
        private NetworkCredential _credentials;

        public DigestAuthenticationMessageHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<TimeSpan?> ShouldRetry(HttpResponseMessage response, int retries, CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var content = await response.Content.ReadAsStringAsync();
                var wwwAuthenticate = response.Headers.WwwAuthenticate.FirstOrDefault();
                if (wwwAuthenticate != null)
                {
                    var scheme = wwwAuthenticate.Scheme;
                    if (scheme == "Digest")
                    {
                        _uri = response.RequestMessage.RequestUri;
                        var server = _uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                        var innerHandler = InnerHandler as HttpClientHandler;
                        if (innerHandler.Credentials != null)
                        {
                            _credentials = innerHandler.Credentials as NetworkCredential;
                            InnerHandler = new HttpClientHandler();
                        }
                        var user = _credentials.UserName;
                        var password = _credentials.Password;
                        _authentication = new DigestAuthentication(server, user, password);
                        _method = response.RequestMessage.Method.Method;
                        _parameter = wwwAuthenticate.Parameter;
                        return TimeSpan.Zero;
                    }
                }
            }
            return null;
        }

        protected override async Task<HttpRequestMessage> CopyRequestMessage(HttpRequestMessage request)
        {
            var message = await base.CopyRequestMessage(request);
            var realm = _authentication.GrabHeaderVar("realm", _parameter);
            var nonce = _authentication.GrabHeaderVar("nonce", _parameter);
            var qop = _authentication.GrabHeaderVar("qop", _parameter);

            var cnonce = Guid.NewGuid().ToString("N");
            var cnonceDate = DateTime.Now;

            message.Headers.Authorization = new AuthenticationHeaderValue("Digest", _authentication.GetDigestHeader(_method, _uri.AbsolutePath, _nc, cnonce, cnonceDate, realm, nonce, qop));
            return message;
        }
    }
}
