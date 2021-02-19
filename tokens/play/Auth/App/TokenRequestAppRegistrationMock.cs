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

  public class Registering
  {

    public static IRegisterAppResult RegisterApp(
      LoginPasswd registration_admin_pass,
      string app_name,
      Uri    app_url,
      IEnumerable<RegisteredApp> state_registered_apps,
      IEnumerable<LoginPasswd> admin_users
    )
    { 
      var lpComp = Equaliser<LoginPasswd>.New(
        lp => lp.Login,
        lp => lp.Password
      );
      if(
        registration_admin_pass == null
        || admin_users.Contains(registration_admin_pass, lpComp) == false
      )
      {
        return new RegisterAppFail(){
          Message = "Wrong admin pass.",
          State = state_registered_apps,
        };
      }
      if(
        string.IsNullOrWhiteSpace(app_name)
        || state_registered_apps.FirstOrDefault(
             rapp => rapp.Name == app_name
           ) != null
      )
      {
        return new RegisterAppFail(){
          Message = "App name must be non empty, non white space, and unique.",
          State = state_registered_apps,
        };
      }

      var pass = GenerateUniqueLoginPasswd(
        state_registered_apps.Select(rapp => rapp.Pass)
      );
      var newRegisteredApp = new RegisteredApp() {
        Name = app_name,
        Pass = pass,
        Url = app_url,
      };
      return new RegisterAppSuccess() {
        App = newRegisteredApp,
        State = state_registered_apps.Append(newRegisteredApp),
      };

    }

    public static LoginPasswd GenerateUniqueLoginPasswd(
      IEnumerable<LoginPasswd> existingPasses,
      int loginBytes = 12, 
      int passwordBytes = 32,
      int maxRetries = 3
    )
    {
      var lp = GenerateLoginPasswd(loginBytes, passwordBytes);
      var lpComp = Equaliser<LoginPasswd>.New(
        lp => lp.Login,
        lp => lp.Password
      );
      var notUnique = existingPasses.Contains(lp, lpComp);
      while(notUnique && maxRetries > 0)
      {
        lp = GenerateLoginPasswd(loginBytes, passwordBytes);
        notUnique = existingPasses.Contains(lp, lpComp);
        maxRetries = maxRetries -1;
      }
      return notUnique ? null : lp;
    }

    public static LoginPasswd GenerateLoginPasswd(
      int loginBytes = 12, 
      int passwordBytes = 32
    )
    {
      var rngLoginBuff = new byte[loginBytes];
      var rngPasswordBuff = new byte[passwordBytes];
      using (var rng = RandomNumberGenerator.Create())
      {
        rng.GetBytes(rngLoginBuff);
        rng.GetBytes(rngPasswordBuff);
        return new LoginPasswd() {
          Login = Encoding.UTF8.GetString(rngLoginBuff),
          Password = Encoding.UTF8.GetString(rngPasswordBuff),
        };
      }
    }

    public interface IRegisterAppResult {}
    public class RegisterAppSuccess : IRegisterAppResult 
    {
      public RegisteredApp App;
      public IEnumerable<RegisteredApp> State;
    }
    public class RegisterAppFail : IRegisterAppResult 
    {
      public string Message = "Cannot register such app access.";
      public IEnumerable<RegisteredApp> State;
    }
    

  }

}