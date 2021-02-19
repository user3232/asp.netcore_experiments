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





  #region Registration

  public interface ILoginPass
  {
    string Login    {get;}
    string Password {get;}
  }

  public static class Registering
  {

    /* 
      registration_endpoint
      here new (apps) passes with scopes are registered
      for passes having registration scope
      stored as entity pass
     */
    public static void RegisterTimedScopedAccessPass()
    {

    }

    /* 
      authorization_endpoint
      here access is requested
     */
    public static void RequestTimedSopedAccessPass(
      string whatResponseWanted,      // 
                                      // Possibilities:
                                      //  response_type = code
                                      //  response_type = id_token 
                                      //  response_type = id_token token
      string passData
                                      // Possibilities:
                                      //  Direct:
                                      //    Login-Password
                                      //    client_id client_secret
                                      //    token
                                      //  Indirect (use User browser and redirects) 
                                      //    session secret
                                      //    login - consent screen

    )
    {

    }

    // authorization_endpoint
    //   this may return code (if pass specified) 
    //   and redirect url (which will be called automaticall
    //   but also can be called by script or clicked)
    // 
    //   or consent which must be clicked and then redirect or click
    //   or even perhaps iframe messages. in this case login (client_id) must
    //   match user (of this client_id)
    // 
    public static void RequestTimedSopedAccessPass_Code_LogPass (
      string                                      login,
      string                                      password,
      string                                      scope,
      string                                      whatStateReturnWithCode,
      string                                      whatNoncePutInToken,
      string                                      whereRequestByRedirectWithAddedCode,
      IEnumerable<ILoginPass>                     registeredPasses,
      Dictionary<ILoginPass, IEnumerable<string>> passToScopeMap,
      IEnumerable<(string code, string token)>    codeToTokenMap,
      Dictionary<ILoginPass, string>              passToSubMap
    )
    {
      var loginPass = registeredPasses
        .FirstOrDefault(
          predicate: lp => lp.Login == login
        );
      // login exists
      if(loginPass == null) return;
      // login and password matches
      if(loginPass.Password != password) return;
      // pass is allowed to request for tokens
      if(
        passToScopeMap.TryGetValue(loginPass, out var scopes) == false
        || scopes.Contains("requestingTokens") == false
      ) return;

      // generate code
      var code = new byte[16];
      RandomNumberGenerator.Fill(code.AsSpan());
      var codeSerial = Encoding.UTF8.GetString(code);

      // generate token
      var token = "token";

      // update state:
      //   add new code-token pair
      //   clean expired code-token pairs
      //   maybe reuse valid earlier requested code-token or clean those
      var newCodeToTokenMap = codeToTokenMap.Append((codeSerial, token));

      // return code
    }

    public static void StoreCode(){}
    public static void PageFromCode(){}
    public static void PageAndActionFromCodeAndAction(){}
    public static void PageAndRedirWithCodeFromCodeReq(){}
    public static void PageAndRedirWithCodeOrRedirWithCodeFromCodeReq(){}
    public static void CodeFromCodeReq(){}


    /* 
      Successful result is either:
        * Login   -> Consent     -> Redir with Code
        * Login   -> (Consented) -> Redir with Code
        * Session -> Consent     -> Redir with Code
        * Session -> (Consented) -> Redir with Code
     */
    public static void RedirWithCodeByLoginConsentOrLoginConsentedOrSessionConsentOrSessionConsented(){}


    public static void xxx(
      string authServTenant,      // auth service tenant (owner) login 
                                  // (encoded is server url)
      string client_id,           // auth service tenant (owner) registered app id,
                                  // note:
                                  //    resource server can have multiple client_id's
                                  //    only for multiple tenants, in different words
                                  //    client_id is server and not server+user,
                                  //    from client_id user cannot can be implied.
                                  // this is identifier known to res serv used to
                                  // provide its users possibility to use external server
                                  //
                                  // client_id is unique per auth server and may be public
                                  //
                                  // client_id + client_secret are known only 
                                  // by auth serv users (tenants, admins)
                                  //  
      string tokenStorageAddress, // app endpoint accepting code
                                  // this endpoint can request token
                                  // in exchange for code and client_secret
      string scope,               // access to what is requested?

      string userLogin,           // needed when consenting tenant app allowanec
      string userPassword         // for obtaining codes and tokens
    )
    {

    }


    /* 
      token_endpoint
      from here tokens can be taken
     */
    public static void GetTimedSopedAccessPass(
      // withReqestCode
      // tlsConnection
      // requestorAddress = url | unverifiedIpAndRedirectUrl
      // requestSecret
      // requestorNonce | requestorState
      // RegisteredTimedScopedAccessPasses
    )
    {

    }

    /* 
      discovery_endpoint
      .../.well-known/openid-configuration
     */
    public static void GetAccessInformations()
    {

    }

    /* 
      introspection_endpoint
      /tokeninfo
      online verification if token is proper
      mainly used for debugging
     */
    public static void VerifyTokenIsValid(){}

    /* 
      userinfo_endpoint
      gets informations about user
      usage requires access token
     */
    public static void GetUserInfo(

    ) {}
  }

  #endregion
}