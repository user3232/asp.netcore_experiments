using System;                           // UriBuilder
                                        // Uri
using System.Collections.Generic;       // List, Dictionary
using System.Linq;                      // IEnumerable


using System.Text;                      // Encoding
using System.Security.Cryptography;     // RSACryptoServiceProvider
                                        // RSAParameters
                                        // RSA
                                        // ECDsa


namespace play.Auth.App
{

  public class Consenting
  {

    public static IConsentAppResult ConsentApp(
      LoginPasswd userPass,           // auth server user login-password
      string request_code,            // request to which it referes
      bool   accept_request,          // request is to be accepted or rejected
      IEnumerable<string> selected_scopes,         // list of accepted scopes
      IEnumerable<ConsentedApp> state_consented,   // server consented apps
      IEnumerable<ConsentRequest> state_requested, // server consent requests
      IEnumerable<LoginPasswd> server_users
    )
    {
      var passesComparer = Equaliser<LoginPasswd>.New(lp => lp.Login, lp => lp.Password);
      if(
        userPass == null              // no user
        || server_users.Contains(userPass, passesComparer) == false
                                      // specified user does not exists
      )
      {
        return new ConsentAppResultError(){
          State = state_consented,
          Message = "Nonexistient user."
        };
      }
      var request = state_requested.FirstOrDefault(
        r => r.ExpectedCode == request_code
      );
      if(
        request == null               // refered consent request not exists
      )
      {
        return new ConsentAppResultError(){
          State = state_consented,
          Message = "Specified consent request does not exists."
        };
      }
      if(
        accept_request == false            // consent request rejected
        || selected_scopes == null         // no scopes selected
        || selected_scopes.Any() == false  // no scopes selected
      )
      {
        return new ConsentAppResultRejected(){
          State = state_consented,
        };
      }
      var resultingScopes = request.Scopes.Intersect(selected_scopes);
      if(
        resultingScopes.Any() == false
      )
      {
        return new ConsentAppResultError(){
          State = state_consented,
          Message = "Bad scopes selected."
        };
      }

      var rngLoginBuff = new byte[12];
      var rngPasswordBuff = new byte[32];
      var rng = RandomNumberGenerator.Create();
      rng.GetBytes(rngLoginBuff);
      rng.GetBytes(rngPasswordBuff);
      var newUserAppPass = new LoginPasswd() {
        Login = Encoding.UTF8.GetString(rngLoginBuff),
        Password = Encoding.UTF8.GetString(rngPasswordBuff),
      };
      var consentedApp = new ConsentedApp(){
        App = request.AppPass,
        ExchangeCode = request.ExpectedCode,
        Scopes = resultingScopes,
        User = userPass.Login,
        Pass = newUserAppPass
      };

      return new ConsentAppResultAccepted(){
        State = state_consented.Append(consentedApp),
        UserConsentedApp = consentedApp
      };
    }

    public interface IConsentAppResult {}
    public class ConsentAppResultAccepted : IConsentAppResult
    {
      public IEnumerable<ConsentedApp> State;
      public ConsentedApp UserConsentedApp;
    }
    public class ConsentAppResultRejected : IConsentAppResult
    {
      public IEnumerable<ConsentedApp> State;
    }
    public class ConsentAppResultError : IConsentAppResult
    {
      public IEnumerable<ConsentedApp> State;
      public string Message;
    }

  }

}