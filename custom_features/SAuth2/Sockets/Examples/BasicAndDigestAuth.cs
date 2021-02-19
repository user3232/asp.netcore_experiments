using Base = System;  
using Net = System.Net;  
using Cache = System.Net.Cache;  
using Io = System.IO;  


namespace SAuth2.Sockets.Examples.Authentication
{
  public static class Authentications
  {
    // 
    public static void AuthenticateBasic()
    {

      string MyURI = "http://localhost:5000/test-auth";  
      // string MyURI = "http://www.contoso.com/";  
      Net.WebRequest webReq = Net.WebRequest.Create(MyURI);  
      webReq.Credentials = new Net.NetworkCredential(
        userName: "UserName", 
        password: "SecurelyStoredPassword"
      );

      // var resp = webReq.GetResponse();
      // var respStream = resp.GetResponseStream();
      using Net.WebResponse resp = webReq.GetResponse();
      using Io.Stream respStream = resp.GetResponseStream();
      using Io.StreamReader respReader = new Io.StreamReader(respStream);

      var responseText = respReader.ReadToEnd();

      Base.Console.WriteLine("Response is:\n" + responseText);
    }
  }
}