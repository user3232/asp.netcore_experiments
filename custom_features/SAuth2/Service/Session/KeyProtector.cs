#nullable enable

using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Session;
using System.Collections.Generic;
// using Microsoft.AspNetCore.Http;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using ResponseHeaders = Microsoft.AspNetCore.Http.Headers.ResponseHeaders;
using Debug = System.Diagnostics.Debug;
using System.Linq;

using WebEncoders = Microsoft.AspNetCore.WebUtilities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SAuth2.Service.Session
{

  public class KeyRawAndProtected
  {
    public string Raw         = "";
    public string Protected   = "";
  }

  public static class KeyRawAndProtectedEx
  {

    public static void Deconstruct(
      this KeyRawAndProtected rawAndProtected, 
      out string raw,
      out string prot
    )
    {
      raw = rawAndProtected.Raw;
      prot = rawAndProtected.Protected;
    }
    
  }
  

  public class KeyProtector
  {
    public Signer Signer {get; private set;}
    public SessionKeyGenerator KeyGenerator {get; private set;}

    public KeyProtector(Signer signer, SessionKeyGenerator keyGenerator)
    {
      Signer = signer;
      KeyGenerator = keyGenerator;
    }
    public KeyProtector() : this(new Signer(), new SessionKeyGenerator()) 
    {
    }

    
    public KeyRawAndProtected CreateAndProtect()
    {
      var key = KeyGenerator.GenerateBase64Url();
      var val = Signer.CreateTextDotSignatureUrlSave_FromBase64Url(key)!;

      return new KeyRawAndProtected {Raw = key, Protected = val};
    }

    public KeyRawAndProtected? Unprotect(string requestStringToCheck)
    {

      // check dot structure:
      var reqTextDotSign = Signer.GetDotStructureIfValid(requestStringToCheck);
      if(reqTextDotSign == null) return null;


      // check key structure:
      // generated keys are url safe, so they would not be transformed:
      var reqText = reqTextDotSign[0];
      if(KeyGenerator.IsConforming(reqText) == false) return null;
      

      // (optionally) check sign structure (SHA256 = 64B => ~86 Base64 )
      var reqSign = reqTextDotSign[1];
      if(reqSign.Length > 86) return null;


      // check text and signature, if valie text is key:
      var key = Signer.GetTextIfVerified(
        messageTextBytes: Encoding.UTF8.GetBytes(reqText), 
        messageSignBytes: WebEncoders.Base64UrlTextEncoder.Decode(reqSign)
      );
      if(key == null) return null;


      // everything ok, fill values:
      return new KeyRawAndProtected {Raw = key, Protected = requestStringToCheck};
    }
  }

}
#nullable restore