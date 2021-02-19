using System;                           // UriBuilder
                                        // Uri
using System.Collections.Generic;       // List, Dictionary


namespace play.Auth.App
{

  public class ConsentRequest
  {
    public LoginPasswd          AppPass;
    public string               User;
    public IEnumerable<string>  Scopes;
    public string               ExpectedCode;
  }

  public class ConsentedApp
  {
    public LoginPasswd          App;
    public string               User;
    public IEnumerable<string>  Scopes;
    public LoginPasswd          Pass;
    public string               ExchangeCode;
  }

  public interface IRegisteredApp {}
  public class RegisteredApp : IRegisteredApp
  {
    public string Name;
    public LoginPasswd Pass;
    public Uri Url;
  }


  public class LoginPasswd
  {
    public string Login {get;set;}
    public string Password {get;set;}
  }

}