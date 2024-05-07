using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSOWithPing.Helper
{
    public class AuthenticationHelper
    {
        // Generates a secure, random code verifier
        public static string GenerateCodeVerifier()
        {
            const int size = 32;
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[size];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes)
                              .TrimEnd('=')
                              .Replace('+', '-')
                              .Replace('/', '_');
            }
        }

        // Generates a code challenge from a given code verifier
        public static string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Convert.ToBase64String(bytes)
                              .TrimEnd('=')
                              .Replace('+', '-')
                              .Replace('/', '_');
            }
        }

        public static string CreateAuthorizationUrl(string clientId, string redirectUri, string codeChallenge)
        {
            return $"https://auth.pingone.com/86b8fad2-8f13-4c8d-93b4-6c9affb63b20/as/authorize?" +
           $"response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
           $"&code_challenge={codeChallenge}&code_challenge_method=S256" +
           $"&scope=openid%20email%20profile&prompt=login";
        }
       

    }
    public static class AuthenticationState
    {
        public static bool IsAuthenticated { get; set; } = false; // default to false
    }
}
