using CSharpFunctionalExtensions;
using Jose;
using Microsoft.AspNetCore.Http;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace API.Middleware
{
    public static class Security
    {
        public static Result<string, Error> Authenticate(HttpRequest request)
            => ExtractJWT(request)
                .Bind(DecodeJWT)
                .Bind(ValidateJWT);

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
            var publicKey = System.Environment.GetEnvironmentVariable("JwtPublicKey");
            if (string.IsNullOrWhiteSpace(publicKey))
                return Pipeline.InternalServerError($"Missing environment variable: 'JwtPublicKey'");

			try
			{
                var csp = ImportPublicKey(publicKey);
				var jwt = JWT.Decode<UaaJwt>(token, csp, JwsAlgorithm.RS256);
                return Pipeline.Success(jwt);
                
			} 
            catch
			{
                return Pipeline.Unauthorized();
			}
        }

        private static Result<string,Error> ValidateJWT(UaaJwt jwt)
        {
            // check for expired token
            if(DateTime.UtcNow > Epoch.AddSeconds((float)(jwt.exp)))
                return Pipeline.Unauthorized();

            // check for unripe token
            if(DateTime.UtcNow < Epoch.AddSeconds((float)jwt.nbf))
                return Pipeline.Unauthorized();

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

		class UaaJwt{
			public string user_name { get; set; }
			// when this token expires, as a Unix timestamp
            public long exp { get; set; }
            // when this token becomes valid, as a Unix timestamp
            public long nbf { get; set; }
		}
    }
}
