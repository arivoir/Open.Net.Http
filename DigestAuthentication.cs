using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Open.Net.Http
{
    internal class DigestAuthentication
    {
        private static string _host;
        private static string _user;
        private static string _password;

        public DigestAuthentication(string host, string user, string password)
        {
            _host = host;
            _user = user;
            _password = password;
        }

        private string CalculateMd5Hash(string input)
        {
            var inputBytes = ASCIIEncoding.ASCII.GetBytes(input);
            var hashAlgorithm = MD5.Create();
            var hash = hashAlgorithm.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public string GrabHeaderVar(string varName, string header)
        {
            var regHeader = new Regex(string.Format(@"{0}=""([^""]*)""", varName));
            var matchHeader = regHeader.Match(header);
            if (matchHeader.Success)
                return matchHeader.Groups[1].Value;
            throw new Exception(string.Format("Header {0} not found", varName));
        }

        //mode="GET"
        public string GetDigestHeader(string mode, string dir, int nc, string cnonce, DateTime cnonceDate, string realm, string nonce, string qop)
        {
            nc = nc + 1;

            var ha1 = CalculateMd5Hash(string.Format("{0}:{1}:{2}", _user, realm, _password));
            var ha2 = CalculateMd5Hash(string.Format("{0}:{1}", mode, dir));
            var digestResponse =
                CalculateMd5Hash(string.Format("{0}:{1}:{2:00000000}:{3}:{4}:{5}", ha1, nonce, nc, cnonce, qop, ha2));

            return string.Format("username=\"{0}\",realm=\"{1}\",nonce=\"{2}\",uri=\"{3}\"," +
                "algorithm=\"MD5\",cnonce=\"{4}\",nc={5:00000000},qop=\"{6}\",response=\"{7}\"",
                _user, realm, nonce, dir, cnonce, nc, qop, digestResponse);
        }
    }
}
