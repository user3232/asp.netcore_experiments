using System;                           // UriBuilder
                                        // Uri
using System.Collections.Generic;       // List, Dictionary
using System.Linq;                      // IEnumerable
using System.Security.Cryptography;     // RSACryptoServiceProvider
                                        // RSAParameters
                                        // RSA
                                        // ECDsa


namespace play.Auth.App
{

  public class Requesting
  {

    public static (IRequestConsentResult Result, IEnumerable<ConsentRequest> NewConsentRequests) RequestConsent(
      LoginPasswd app_registered_pass,  // app signature (url+cert or login+password)
      string user_login,                // who should consent? 
      IEnumerable<string> scopes,       // requested scopes
      IEnumerable<ConsentRequest> state_consent_reqests, 
                                        // current consents
      IEnumerable<LoginPasswd> registered_passes,
      IEnumerable<string> users_logins,
      IEnumerable<string> available_scopes
    )
    {
      // no app with provided identity
      if(
        app_registered_pass == null 
        || string.IsNullOrWhiteSpace(app_registered_pass.Login)
        || string.IsNullOrWhiteSpace(app_registered_pass.Password)
        || registered_passes.Any(
              pass => app_registered_pass.Login == pass.Login 
                      && app_registered_pass.Password == pass.Password
           ) == false
      )
      {
        return (
          Result: new RequestConsentResultError(){Message = "Wrong app"},
          NewConsentRequests: state_consent_reqests
        );
      }
      // no user with provided login
      if(
        string.IsNullOrWhiteSpace(user_login)
        || users_logins.Contains(user_login) == false
      )
      {
        return (
          Result: new RequestConsentResultError(){Message = "User does not exists."},
          NewConsentRequests: state_consent_reqests
        );
      }
      // no scopes to be applied
      if(
        scopes == null 
        || available_scopes.Intersect(scopes).Any() == false
      )
      {
        return (
          Result: new RequestConsentResultError(){Message = "Wrong scopes."},
          NewConsentRequests: state_consent_reqests
        );
      }
      
      var expectedCode = RandomNumberGenerator.GetInt32(int.MaxValue).ToString();
      // Success
      return (
        Result: new RequestConsentResultCode(){
          Code = expectedCode
        }, 
        NewConsentRequests: state_consent_reqests.Append(
          new ConsentRequest() {
            AppPass = app_registered_pass,
            Scopes = available_scopes.Intersect(scopes),
            User = user_login,
            ExpectedCode = expectedCode
          }
        )
      );
    }
    public interface IRequestConsentResult {}
    public class RequestConsentResultCode : IRequestConsentResult
    {
      public string Code;
    }
    public class RequestConsentResultError : IRequestConsentResult
    {
      public string Message = "Unregistered app type.";
    }



    




    public static IRequestConsentedPassResult RequestConsentedPass(
      LoginPasswd app_registered_pass, // app signature (url+cert or login+password)
      string consent_request_code,     // proof of possesion
      IEnumerable<ConsentedApp> consented_apps, // server consented apps
      IEnumerable<LoginPasswd> registered_passes
    )
    {
      if(
        app_registered_pass == null 
        || string.IsNullOrWhiteSpace(app_registered_pass.Login)
        || string.IsNullOrWhiteSpace(app_registered_pass.Password)
        || registered_passes.Any(
              pass => app_registered_pass.Login == pass.Login 
                      && app_registered_pass.Password == pass.Password
           ) == false
      )
      {
        return new RequestConsentedPassResultError(){
          Message = "Wrong app type."
        };
      }
      if(
        string.IsNullOrWhiteSpace(consent_request_code)
        || consented_apps.Any(
            app => app.ExchangeCode == consent_request_code
          ) == false
      )
      {
        return new RequestConsentedPassResultError(){
          Message = "Wrong code."
        };
      }
      
      // Success
      var consent = consented_apps.First(
            app => app.ExchangeCode == consent_request_code
          );
      return new RequestConsentedPassResultPass(){
        ContentedPass = consent.Pass,
        Scopes = consent.Scopes
      };
    }
    public interface IRequestConsentedPassResult {}
    public class RequestConsentedPassResultPass : IRequestConsentedPassResult
    {
      public LoginPasswd ContentedPass;
      public IEnumerable<string> Scopes;
    }
    public class RequestConsentedPassResultError : IRequestConsentedPassResult
    {
      public string Message = "Something wrong...";
    }

    







    public static IRequestTokenResult RequestToken (
      LoginPasswd consented_pass,     // app login-password
                                      // (app may have many login-passwords
                                      // for many auth server users)
      IEnumerable<string> required_scopes, 
                                      // registered scopes subset required
                                      // for current task
      IEnumerable<ConsentedApp> state_consented
    )
    {
      var userApp = state_consented.FirstOrDefault(
        ca => 
          ca.Pass.Login == consented_pass.Login
          && ca.Pass.Password == consented_pass.Password
      );
      if(userApp == null) // error
      {
        return new RequestTokenResultError() {
          Message = "Invalid consent login-password."
        };
      }
      if(required_scopes == null || required_scopes.Any() == false)
      { 
        return new RequestTokenResultError() {
          Message = "No scopes requested."
        };
      }
      var userScopes = userApp.Scopes.Intersect(required_scopes);
      if(userScopes.Any() == false)
      {
        return new RequestTokenResultError() {
          Message = "Invalid scopes requested."
        };
      }

      // generate token...
      
      return new RequestTokenResultToken(){
        Token = "This is token Base64UrlEncoded."
      };
      
    }
    public interface IRequestTokenResult {}
    public class RequestTokenResultError : IRequestTokenResult
    {
      public string Message;
    }
    public class RequestTokenResultToken : IRequestTokenResult
    {
      public string Token;
    }
   
  }

}