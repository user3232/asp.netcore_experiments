using System;                           // UriBuilder
using System.Collections.Generic;       // List, Dictionary
using System.Linq;                      // IEnumerable
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

namespace play.Auth.Tokens
{

  public static class Identify
  {
    

    public static (bool, object)    ReqInMutualTlsEnv<TReq>(
      TReq req,
      Func<TReq, bool> selector
    ) => (selector(req), null);


    public static (bool, object)    ReqInTlsEnv<TReq>(
      TReq req,
      Func<TReq, bool> selector
    ) => (selector(req), null);


    public static (bool, LoginPass) ReqHaveValidLoginPass<TReq>(
      TReq req, 
      Func<TReq, (string login, string pass)> selector,
      IEnumerable<LoginPass> loginPassSet
    ) => loginPassSet.FirstOrDefault(
        predicate:  lp =>    lp.Login == selector(req).login 
                          && lp.Pass == selector(req).pass,
        success:    lp => (true, lp),
        failure:    (false, null)
      );


    public static (bool, Uri) MatchUrl<TReq>(
      TReq req,
      Func<TReq, string> selector,
      IEnumerable<Uri> allowedUrls
    ) => allowedUrls.FirstOrDefault(
      predicate: url => url.Authority == selector(req),
      success:   url => (true, url),
      failure:          (false, null)
    );

    // Match cert
    // distinguish on cert subject disthinguished name
    // or alternative name dns
    public static (bool, X509Certificate2)? ReqHaveValidCert<TReq>(
      TReq req,
      Func<TReq, X509Certificate2> select,
      X509Certificate2[] trustedCertificates
    ) 
    {
      var cert = select(req);
      using (var chain = new X509Chain())
      {
        chain.ChainPolicy.ExtraStore.AddRange(trustedCertificates);
                                      // extra certificates used to build chain
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                                      // dont check for revocation
                                      // this is simple example, otherwise
                                      // server with revocation should 
                                      // be specified.
        // chain.ChainPolicy.DisableCertificateDownloads = true;
                                      // Net 5.0
        var isValid = chain.Build(cert);
        return (isValid, cert);
      }
    }


    public static (bool, X509Certificate2) ReqHaveOnlyPrivateTrustedCert<TReq>(
      TReq req,
      Func<TReq, X509Certificate2> select,
      X509Certificate2[] trustedCertificates
    ) 
    {
      // Some garbage:
      //   https://www.meziantou.net/custom-certificate-validation-in-dotnet.htm
      //   https://stackoverflow.com/questions/7331666/c-sharp-how-can-i-validate-a-root-ca-cert-certificate-x509-chain/7332193#7332193
      // Some references:
      //   https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2
      //   https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509chain
      //   https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509verificationflags
      //   https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509chainpolicy
      //   https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.storename
      //   https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.storelocation
      // Net 5.0 ChainPolicy have:
      //   X509ChainPolicy.CustomTrustStore.AddRange(...)
      //   X509ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust
      //   X509ChainPolicy.DisableCertificateDownloads = true
      //   bool result = X509Chain.Build(cert)
      // Signature verification:
      //   https://stackoverflow.com/questions/56660195/how-to-verify-digital-signature-in-c-sharp
      // Certificate generation examples:
      //   https://gist.github.com/svrooij/ec6f664cd93cd09e84414112d23f6a42
      // Certificate proper public key retrival:
      //   https://www.pkisolutions.com/accessing-and-using-certificate-private-keys-in-net-framework-net-core/
      // Certificates reading (.Net 5.0) 
      // for earlier versions prepare certificate in cer format..:
      //   https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.createfrompem
      //   https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.createfrompemfile
      var cert = select(req);
      
      using (var chain = new X509Chain())
      {
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                                        // dont check for revocation
                                        // this is simple example, otherwise
                                        // server with revocation should 
                                        // be specified.
        chain.ChainPolicy.ExtraStore.AddRange(trustedCertificates);
                                        // extra certificates used to build chain

        if(chain.Build(cert) == false)  // check validity with system and private 
          return (false, null);         // trusted certs
        

        foreach(var chainElement in chain.ChainElements)
        {                               
                                        // Now, every cert in the chain
                                        // (besides oryginal) have to
                                        // be certified by one of trustedCertificates.
          if(chainElement.Certificate.Equals(cert))
            continue;
          var fromTrusted = trustedCertificates
            .Select(tc => tc.Thumbprint)
              .Contains(chainElement.Certificate.Thumbprint);
          if(fromTrusted == false)
            return (false, cert);
        }
        
        return (true, cert);
      }
    }


    public static bool ValidateSignature<TReq>(
      TReq req,
      Func<TReq, string> selectJwt,
      string jwk
    )
    {
      /* 
        https://stackoverflow.com/questions/34403823/verifying-jwt-signed-with-the-rs256-algorithm-using-public-key-in-c-sharp

        
        jwt(decoded): 

          {
            "kid":"1e9gdk7",
            "alg":"RS256"
          }.
          {
            "iss": "http://server.example.com",
            "sub": "248289761001",
            "aud": "s6BhdRkqt3",
            "nonce": "n-0S6_WzA2Mj",
            "exp": 1311281970,
            "iat": 1311280970,
            "c_hash": "LDktKdoQak3Pk0cnXxCltA"
          }.
          signature
        
        jwt: 

          eyJraWQiOiIxZTlnZGs3IiwiYWxnIjoiUlMyNTYifQ.
          ewogImlzcyI6ICJodHRwOi8vc2VydmVyLmV4YW1wbGU
          uY29tIiwKICJzdWIiOiAiMjQ4Mjg5NzYxMDAxIiwKIC
          JhdWQiOiAiczZCaGRSa3F0MyIsCiAibm9uY2UiOiAib
          i0wUzZfV3pBMk1qIiwKICJleHAiOiAxMzExMjgxOTcw
          LAogImlhdCI6IDEzMTEyODA5NzAsCiAiY19oYXNoIjo
          gIkxEa3RLZG9RYWszUGswY25YeENsdEEiCn0.
          XW6uhdrkBgcGx6zVIrCiROpWURs-4goO1sKA4m9jhJI
          ImiGg5muPUcNegx6sSv43c5DSn37sxCRrDZZm4ZPBKK
          gtYASMcE20SDgvYJdJS0cyuFw7Ijp_7WnIjcrl6B5cm
          oM6ylCvsLMwkoQAxVublMwH10oAxjzD6NEFsu9nipks
          1zWhsPePf_rM4eMpkmCbTzume-fzZIi5VjdWGGEmzTg
          32h3jiex-r5WTHbj-u5HL7u_KP3rmbdYNzlzd1xWRYT
          Us4E8nOTgzAUwvwXkIQhOh5TPcSMBYy6X3E7-_gr9Ue
          6n4ND7hTFhtjYs3cjNKIA08qm5cpVYFMFMG6PkhzLQ
        
        jwk:

          {
            "kty":"RSA",
            "kid":"1e9gdk7",
            "n":"w7Zdfmece8iaB0kiTY8pCtiBtzbptJmP28nSWw
                 tdjRu0f2GFpajvWE4VhfJAjEsOcwYzay7XGN0b
                 -X84BfC8hmCTOj2b2eHT7NsZegFPKRUQzJ9wW8
                 ipn_aDJWMGDuB1XyqT1E7DYqjUCEOD1b4FLpy_
                 xPn6oV_TYOfQ9fZdbE5HGxJUzekuGcOKqOQ8M7
                 wfYHhHHLxGpQVgL0apWuP2gDDOdTtpuld4D2LK
                 1MZK99s9gaSjRHE8JDb1Z4IGhEcEyzkxswVdPn
                 dUWzfvWBBWXWxtSUvQGBRkuy1BHOa4sP6FKjWE
                 eeF7gm7UMs2Nm2QUgNZw6xvEDGaLk4KASdIxRQ",
            "e":"AQAB"
          }

       */


      var jwt          = selectJwt(req);
      var jwtDotParts  = jwt.Split(".");
      var jwtHeader    = jwtDotParts[0];
      var jwtPayload   = jwtDotParts[1];
      var jwtSignature = jwtDotParts[2];

      var jwkDict = JsonSerializer.Deserialize<Dictionary<string,string>>(jwk);
      // assert(jwkDict["kty"] == "RSA")
      // assert(jwkDict["kid"] == "1e9gdk7")
      var exponent = BytesFromBase64Url(jwkDict["e"]);
      var modulus = BytesFromBase64Url(jwkDict["n"]);

      RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
      rsa.ImportParameters(
        new RSAParameters() {
          Modulus = modulus,
          Exponent = exponent
        });

      SHA256 sha256 = SHA256.Create();
      byte[] hash = sha256.ComputeHash(
        Encoding.UTF8.GetBytes(jwtHeader + '.' + jwtPayload)
      );

      RSAPKCS1SignatureDeformatter rsaDeformatter = 
        new RSAPKCS1SignatureDeformatter(rsa);
      rsaDeformatter.SetHashAlgorithm("SHA256");
      var isValid = rsaDeformatter.VerifySignature(
        hash, 
        BytesFromBase64Url(jwtSignature)
      );

      return isValid;

    }
    
    static int Base64UrlPadding(string base64Url) => base64Url.Length % 4;
    
    static byte[] BytesFromBase64Url(string base64Url)
    {
      var base64UrlPadded = base64Url + new string('=', Base64UrlPadding(base64Url));
      var base64 = base64UrlPadded.Replace("_", "/").Replace("-", "+");
      return Convert.FromBase64String(base64);
    }


    static byte[] BytesFromBase64Url2(string base64Url) => 
      Base64UrlEncoder.DecodeBytes(base64Url);


    public static bool VerifyRsaSignature(
      RSA               rsaPubKey, 
      HashAlgorithmName hashAlgorithmName,
      byte[]            bytesToHashSign,
      byte[]            bytesHashSigned
    ) 
    {
      // var verifier = new RSAPKCS1SignatureDeformatter();
      // verifier.SetKey(key: rsaPubKey);
      // verifier.SetHashAlgorithm(hashAlgorithmName.Name);
      // return verifier.VerifySignature(
      //   rgbHash:bytesToHashSign,
      //   rgbSignature: bytesHashSigned
      // );

      return rsaPubKey.VerifyData(
        data:          bytesToHashSign,
        signature:     bytesHashSigned,
        hashAlgorithm: hashAlgorithmName,
        padding:       RSASignaturePadding.Pkcs1
      );

    }


    public static byte[] GenerateRsaSignature(
      RSA               rsaPrivKey, 
      HashAlgorithmName hashAlgorithmName,
      byte[]            bytesToHashSign
    ) 
    {
      // var signer = new RSAPKCS1SignatureFormatter(key: rsaPrivKey);
      // signer.SetHashAlgorithm(strName: hashAlgorithmName.Name);
      // return signer.CreateSignature(rgbHash: bytesToHashSign);

      // return rsaPrivKey.SignHash(
      //   hash: 
      //     HashAlgorithm
      //       .Create(hashName: hashAlgorithmName.Name)
      //         .ComputeHash(buffer: bytesToHashSign),
      //   hashAlgorithm: hashAlgorithmName,
      //   padding:       RSASignaturePadding.Pkcs1
      // );

      return rsaPrivKey.SignData(
        data:          bytesToHashSign,
        offset:        0,
        count:         bytesToHashSign.Length,
        hashAlgorithm: hashAlgorithmName,
        padding:       RSASignaturePadding.Pkcs1
      );
    }


    public static byte[] GenerateRsaSignature(
      RSA               rsaPrivKey, 
      HashAlgorithmName hashAlgorithmName,
      string            stringToHashSign
    ) => 
      GenerateRsaSignature(
        rsaPrivKey:        rsaPrivKey, 
        hashAlgorithmName: hashAlgorithmName, 
        bytesToHashSign:   Encoding.UTF8.GetBytes(stringToHashSign)
      );


    public static (RSA pubPrivKey, RSAParameters pubConf, RSAParameters pubPrivConf) NewPubPrivRsaKey()
    {
      var rsa = RSA.Create(keySizeInBits: 2048);
      return (
        pubPrivKey:   rsa,
        pubConf:      rsa.ExportParameters(includePrivateParameters: false),
        pubPrivConf:  rsa.ExportParameters(includePrivateParameters: true)
      );
    }

    public static (bool, JwtSecurityToken) ValidateJwtSignature<TReq>(
      TReq req,
      Func<TReq, string> selectJwt,
      string jwk
    )
    {
      var jwt = selectJwt(req);

      try
      {
        // var jwkDict = JsonSerializer.Deserialize<Dictionary<string,string>>(jwk);
        // var modulus = BytesFromBase64Url(jwkDict["n"]);
        // var exponent = BytesFromBase64Url(jwkDict["e"]);
        var jwkO = JsonWebKey.Create(json: jwk);
        var modulus = Base64UrlEncoder.DecodeBytes(jwkO.N);
        var exponent = Base64UrlEncoder.DecodeBytes(jwkO.E);
        
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        rsa.ImportParameters(
          new RSAParameters()
          {
              Modulus = modulus,
              Exponent = exponent
          });
        new JwtSecurityTokenHandler().ValidateToken(
          token: jwt, 
          validationParameters: 
            new TokenValidationParameters
            {
              RequireExpirationTime = true,
              RequireSignedTokens = true,
              ValidateAudience = false,
              ValidateIssuer = false,
              ValidateLifetime = false,
              IssuerSigningKey = new RsaSecurityKey(rsa)
            }, 
          validatedToken: out SecurityToken validatedSecurityToken
        );
        JwtSecurityToken validatedJwt = validatedSecurityToken as JwtSecurityToken;
        return (true, validatedJwt);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        return (false, null);
      }
    }



    public static V FirstOrDefault<T, V>(
      this IEnumerable<T> sequence,
      Func<T,bool> predicate,
      Func<T, V> success,
      V failure
    ) {
      var res = sequence.FirstOrDefault(predicate);
      return res == null ? failure : success(res);
    }
  
  }

  public class LoginPass
  {
    public string Login;
    public string Pass;
  }

  public class Identifor<TReq>
  {
    // identify source    (user-albert@google.com, login-password, google.com)
    // identify target    (user-mike@example.com, example.com, magic num, ...)
    // identify environment (e.g. mutual TLS, does not matter, etc...)
    //   matching identification is associated
    //   with matching user at receiving side.
    //   If few other parameters choose user,
    //   those parameters needs to be added to identifier.
    public Func<TReq, (bool, object)> Ifor;

    // to distinguish answers V may by hash:
    public Func<TReq, (bool, long)> IforHash;

    // storage of identificators distinguished by object values:
    // every type is connected to objects with same type
    // different types may have associated distinguishers of different types.
    public Dictionary<Type, IEnumerable<object>> Identificators;
    public IEnumerable<(Type, object)> Identificators2;
    // sorage of identificators distinguished by hashes:
    // (this unfortunately perevents extracting useful informations)
    public Dictionary<Type, IEnumerable<long>> IdentificatorsHashDistingushed;
  }


}