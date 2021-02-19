using System;                           // UriBuilder
using System.Collections.Generic;       // List, Dictionary
using System.Linq;                      // IEnumerable
using System.Text.RegularExpressions;   // Regex
using System.Collections.Specialized;   // NameValueCollection
using System.Net;                       // WebUtility
using System.Web;                       // HttpUtility
using System.Text;                      // Encoding
using System.Security.Cryptography.X509Certificates;
                                        // 
using System.Security.Cryptography;     // RSACryptoServiceProvider
                                        // RSAParameters
                                        // RSA
                                        // ECDsa
using System.Text.Json;                 // JsonSerializer.Deserialize
using System.IdentityModel.Tokens.Jwt;  // JwtSecurityToken
                                        // JwtSecurityTokenHandler
using Microsoft.IdentityModel.Tokens;   // SecurityToken
                                        // TokenValidationParameters
                                        // SecurityKey
                                        //   AsymmetricSecurityKey
                                        //     ECDsaSecurityKey
                                        //     RsaSecurityKey
                                        //     X509SecurityKey
                                        //   SymmetricSecurityKey
                                        //     CryptoProviderFactory
                                        //       .CreateAuthenticatedEncryptionProvider(
                                        //         SecurityKey, 
                                        //         String
                                        //       )
                                        //   JsonWebKey
using System.Security.Claims;           // Claim
using Microsoft.Identity.Web;           // ClaimConstants
                                        // needs dlls from azure, lots of

namespace play.Auth.Tokens
{



  #region Token generation

  public static class Authorizing
  {
    
    public static string GenerateJwt(
      string issuer,        // "https://localhost:5002/identification",
                            // OpenId Connect 1.0 Required
                            // JSON["iss"] (issuer)
                            // OpenId Provider url
      string audience,      // "qa@mike@localhost:5002"
                            // OpenId Connect 1.0 Required
                            // JSON["aud"] (audience)
                            // client_id -> login (of app)
      string subject,       // mike@jwt_provider.com
                            // OpenId Connect 1.0 Required
                            // JSON["sub"] (subject)
                            // master login (of user)
      string nonce,
                            // OpenId Connect 1.0 Required if requested
                            // JSON["nonce"] (nonce)
                            // Bag for value used for sessions
                            // and replay attacks protection.
                            // Value is set in Authentication Request
                            // and preserved to the token
      DateTime expiration,  // 
                            // OpenId Connect 1.0 Required
                            // JSON["exp"] (expiration)
                            // expiration time
                            // Number of seconds from 1970–01–01T0:0:0Z 
                            // as measured in UTC until the date/time
      DateTime issuedAt,    // 
                            // OpenId Connect 1.0 Required
                            // JSON["iat"] (issued at)
                            // time of issuance
                            // Number of seconds from 1970–01–01T0:0:0Z 
                            // as measured in UTC until the date/time
      IEnumerable<string> scopes,
                            // list of scopes to include
      SecurityKey privateKey
                            // key to use to sign jwt
    )
    {
      
      var toBeJsonSerial = new 
      {
        iss = issuer,
        aud = audience,
        sub = subject,
        nonce = nonce,
        exp   = new DateTimeOffset(         // expires
            dateTime: expiration, 
            offset: TimeSpan.Zero
          ).ToUnixTimeSeconds().ToString(),
        iat   = new DateTimeOffset(         // issued at 
            dateTime: issuedAt, 
            offset: TimeSpan.Zero
          ).ToUnixTimeSeconds().ToString(),
        scp = string.Join(' ', scopes)      // scope
      };
      var jsonPayload = JsonSerializer.Serialize(toBeJsonSerial);

      var jsonPayloadSerial = @$"{{
        ""iss""=""{issuer}"",
        ""aud""=""{audience}"",
        ""sub""=""{subject}"",
        ""nonce""=""{nonce}"",
        ""exp""={new DateTimeOffset(dateTime: expiration, offset: TimeSpan.Zero).ToUnixTimeSeconds()},
        ""iat""={new DateTimeOffset(dateTime: issuedAt, offset: TimeSpan.Zero).ToUnixTimeSeconds()},
        ""scp""=""{string.Join(' ', scopes)}""
      }}";
      
      
      
      var token1 = new JwtSecurityToken(
        header: new JwtHeader(
          signingCredentials: new SigningCredentials(
            // key: new RsaSecurityKey(RSA.Create(keySizeInBits: 2048)),
            key: privateKey,
            algorithm: SecurityAlgorithms.RsaSha256
          )
        ),
        payload: new JwtPayload(
          issuer: issuer,
          audience: audience,
          // claims: Enumerable.Empty<Claim>(),
          claims: new List<Claim>{
            new Claim(
              type: ClaimConstants.Scp, // type: "scp",
              value: string.Join(' ', scopes)
            ),
            new Claim(
              type: ClaimConstants.Sub, // type: "sub",
              value: subject
            )
          },
          notBefore: null,
          expires: expiration,
          issuedAt: issuedAt
        )
      );

      var token2 = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: new List<Claim>{
          new Claim(
            type: ClaimConstants.Scp,
            value: string.Join(' ', scopes) 
          ),
          new Claim(
            type: ClaimConstants.Sub, // type: "sub",
            value: subject
          )
        },
        notBefore: null,
        expires: expiration,
        signingCredentials: new SigningCredentials(
            // key: new RsaSecurityKey(RSA.Create(keySizeInBits: 2048)),
            key: privateKey,
            algorithm: SecurityAlgorithms.RsaSha256
          )
      );

      // new RsaSecurityKey()

      return new JwtSecurityTokenHandler().WriteToken(token: token1);
    }

    // ***********************************
    // Token encryption
    // ***********************************
    // 
    // auth server can ask resource server to encrypt token
    // before sending (encrypted) token to client


  }

  #endregion



}