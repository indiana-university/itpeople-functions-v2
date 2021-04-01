using CSharpFunctionalExtensions;
using Jose;
using Microsoft.AspNetCore.Http;
using Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace API.Middleware
{
    public static class Security
    {
        private static HttpClient TokenClient = new HttpClient();

        public static Result<string, Error> Authenticate(HttpRequest request)
            => SetStartTime(request)
                .Bind(ExtractJWT)
                .Bind(DecodeJWT)
                .Bind(ValidateJWT)
                .Bind(netid => SetPrincipal(request, netid));

        public static Task<Result<UaaJwtResponse, Error>> ExhangeOAuthCodeForToken(HttpRequest request, string code) 
        {
            UaaJwtResponse stashedResp = null;
            return SetStartTime(request)
                .Bind(_ => CreateUaaTokenRequest(code))
                .Bind(PostUaaTokenRequest)
                .Bind(ParseUaaTokenResponse)
                .Tap(resp => stashedResp = resp)
                .Bind(jwt => DecodeJWT(jwt.access_token))
                .Bind(jwt => SetPrincipal(request, jwt.user_name))
                .Bind(_ => Pipeline.Success(stashedResp));
        }

        private static Result<FormUrlEncodedContent, Error> CreateUaaTokenRequest(string code)
        {
            try
            {
                var dict = new Dictionary<string, string>()
                {
                    {"grant_type", "authorization_code"},
                    {"code", code},
                    {"client_id", Utils.Env("OAuthClientId", required: true)},
                    {"client_secret", Utils.Env("OAuthClientSecret", required: true)},
                    {"redirect_uri", Utils.Env("OAuthRedirectUrl", required: true)},
                };
                var content = new FormUrlEncodedContent(dict);
                return Pipeline.Success(content);
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError("Failed to create OAuth2 token exchange request", ex);
            }
        }

        private static async Task<Result<HttpContent, Error>> PostUaaTokenRequest(FormUrlEncodedContent content)
        {
            try
            {
                var url = Utils.Env("OAuthTokenUrl", required: true);
                var resp = await TokenClient.PostAsync(url, content);
                if (!resp.IsSuccessStatusCode)
                {
                    var error = await resp.Content.ReadAsStringAsync();
                    return Pipeline.BadRequest($"Failed to exchange OAuth2 code for access token: {resp.StatusCode} {error}");
                }
                else
                {
                    return Pipeline.Success(resp.Content);
                }
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError("Failed to exchange OAuth2 code for access token", ex);
            }
        }

        private static async Task<Result<UaaJwtResponse, Error>> ParseUaaTokenResponse(HttpContent content)
        {
            try
            {
                var uaaJwt = await content.ReadAsAsync<UaaJwtResponse>();
                return Pipeline.Success(uaaJwt);
            }
            catch (Exception ex)
            {
                return Pipeline.InternalServerError("Failed to parse UAA response as access token.", ex);
            }
        }

        private static Result<HttpRequest, Error> SetStartTime(HttpRequest request)
        {
            request.HttpContext.Items[LogProps.ElapsedTime] = System.DateTime.UtcNow;
            return Pipeline.Success(request);
        }

        internal static Result<UaaJwt,Error> SetPrincipal(HttpRequest request, UaaJwt uaaJwt)
        {
            request.HttpContext.Items[LogProps.RequestorNetid] = uaaJwt.user_name;
            return Pipeline.Success(uaaJwt);
        }

        internal static Result<string,Error> SetPrincipal(HttpRequest request, string netid)
        {
            request.HttpContext.Items[LogProps.RequestorNetid] = netid;
            return Pipeline.Success(netid);
        }

        public const string ErrorRequestMissingAuthorizationHeader = "Request is missing an Authorization header.";
        public const string ErrorRequestEmptyAuthorizationHeader = "Request contains empty Authorization header.";
        public const string ErrorRequestAuthorizationHeaderMissingBearerScheme = "Request Authorization header scheme must be \"Bearer\"";
        public const string ErrorRequestAuthorizationHeaderMissingBearerToken = "Request Authorization header contains empty \"Bearer\" token.";

        private static Result<string,Error> ExtractJWT(HttpRequest request)
        {
            if (!request.Headers.ContainsKey("Authorization")) 
                return Pipeline.BadRequest(ErrorRequestMissingAuthorizationHeader);
            
            var authHeaders = request.Headers["Authorization"].ToArray();
            if (!authHeaders.Any()) 
                return Pipeline.BadRequest(ErrorRequestEmptyAuthorizationHeader);
            
            var header = authHeaders.First();
            if (!header.StartsWith("Bearer", ignoreCase: true, culture: CultureInfo.InvariantCulture)) 
                return Pipeline.BadRequest(ErrorRequestAuthorizationHeaderMissingBearerScheme);

            var bearerToken = header.Replace("Bearer", "", ignoreCase: true, culture: CultureInfo.InvariantCulture).Trim();
            if (string.IsNullOrWhiteSpace(bearerToken)) 
                return Pipeline.BadRequest(ErrorRequestAuthorizationHeaderMissingBearerToken);
            
            return Pipeline.Success(bearerToken);
        }
        private static Result<UaaJwt,Error> DecodeJWT(string token)
        {
            //https://apps.iu.edu/uaa-prd/oauth/token_key
            // "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAp+0OVVhuRqbXeRr9PO1Zo5Az/OCTTK6NKTSPfU87wHvFhl+6vO2h5b9YCdr74edubZ6grPvnHkWFK3SWMVnxB4EUatcnfwLsGwvZz+96QOSa5qEdkaYzCC5oQI6x6VnqT/yzNol9HUKQuT4b6faK8bj7Y86Ku0Bn3msYSYAI4aJxh6KIgO5kbVLMjYHDsABvmJVqG77e8qhJ5aHzHE7voNKAkVBKx3Bqofu9pwT9A5ejBylFrPnhCJK7vQu0SaBB/pHmDb9dD969oWGX6QdoGPbmXuW1FsSsph0bHxiOMLmOTZSFPs2/gFnpSMYwIinRPi3+saiI+GtPbwAf+ZliCQIDAQAB\n-----END PUBLIC KEY-----"
            
            var publicKey = Utils.Env("JwtPublicKey", required: false);
            if (string.IsNullOrWhiteSpace(publicKey))
                return Pipeline.InternalServerError($"Missing environment variable: 'JwtPublicKey'");

            RSACryptoServiceProvider csp = null;
            try
            {
                csp = ImportPublicKey(publicKey.Replace("\\n", "\n"));				
            } 
            catch (Exception ex)
            {
                return Pipeline.InternalServerError("Failed to import JWT public key", ex);
            }

            try
            {
                return Pipeline.Success(JWT.Decode<UaaJwt>(token, csp, JwsAlgorithm.RS256));
            } 
            catch
            {
                return Pipeline.Unauthorized("Failed to decode JWT");
            }
        }

        private static Result<string,Error> ValidateJWT(UaaJwt jwt)
        {
            var nowUnix = System.Math.Floor((DateTime.UtcNow - Epoch).TotalSeconds);

            // check for expired token
            if(nowUnix > (double)jwt.exp)
                return Pipeline.Unauthorized("Access token has expired.");

            // check for unripe token
            if(nowUnix < (double)jwt.nbf)
                return Pipeline.Unauthorized("Access token is not yet valid.");

            return Pipeline.Success(jwt.user_name);
        }

        private static byte[] ConvertRSAParametersField(BigInteger n, int size)
        {
            byte[] bs = n.ToByteArrayUnsigned();

            if (bs.Length == size)
                return bs;

            if (bs.Length > size)
                throw new ArgumentException("Specified size too small", "size");

            byte[] padded = new byte[size];
            Array.Copy(bs, 0, padded, size - bs.Length, bs.Length);
            return padded;
        }

        public static RSAParameters ToRSAParameters(RsaKeyParameters rsaKey)
        {
            RSAParameters rp = new RSAParameters();
            rp.Modulus = rsaKey.Modulus.ToByteArrayUnsigned();
            if (rsaKey.IsPrivate)
                rp.D = ConvertRSAParametersField(rsaKey.Exponent, rp.Modulus.Length);
            else
                rp.Exponent = rsaKey.Exponent.ToByteArrayUnsigned();
            return rp;
        }

        public static RSACryptoServiceProvider ImportPublicKey(string pem)
        {
            PemReader pr = new PemReader(new StringReader(pem));
            AsymmetricKeyParameter publicKey = (AsymmetricKeyParameter)pr.ReadObject();
            RSAParameters rsaParams = ToRSAParameters((RsaKeyParameters)publicKey);
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
            csp.ImportParameters(rsaParams);
            return csp;
        }

        static DateTime Epoch = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);


    }
}
