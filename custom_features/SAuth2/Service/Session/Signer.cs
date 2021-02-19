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


// https://en.wikipedia.org/wiki/Base64
//
// Base64 encoding (RFC 4648 §4: base64 (standard)):
//   * allowed chars: [a-z A-Z 0-9 + / ]
//   * padding char (optional): [=]
// 
// Base64url encoding (RFC 4648 §5: base64url (URL- and filename-safe standard)):
//   * allowed chars: [a-z A-Z 0-9 - _ ]
//   * padding char (optional): [=]
//
// https://en.wikipedia.org/wiki/Percent-encoding
//
// Url encoding:
//   * Reserved Characters (RFC 3986 section 2.2):
//       ! 	# 	$ 	& 	' 	( 	) 	* 	+ 	, 	/ 	: 	; 	= 	? 	@ 	[ 	]
//   * Unreserved Characters:
//       A-Z a-z 0-9 - 	_ 	. 	~ 
//   * Other chars must be escaped using percent-encoding
//
// Url save encoding:
//   * Url encoding unreserved chars are left as they are
//   * Reserved chars and other chars are escaped using percent encoding
//   
// Percent encoding:
//   * Percent-encoding a reserved character involves converting the character
//     to its corresponding byte value in ASCII and then representing that 
//     value as a pair of hexadecimal digits. 
//   * The digits, preceded by a percent sign (%) which is used as an 
//     escape character, are then used in the URI in place of the reserved 
//     character. 
//   * (For a non-ASCII character, it is typically converted to its byte 
//     sequence in UTF-8, and then each byte value is represented as above.)

namespace SAuth2.Service.Session
{

  public class Signer
  {
    public HMACSHA256 Algorithm;
    public Signer() { Algorithm = new HMACSHA256(); }
    public Signer(byte[] signingPrivateKey) 
    { 
      Algorithm = new HMACSHA256(signingPrivateKey); 
    }

    
    

    public string? CreateDataDotSignatureUrlSave<T>(T typedObject)
    {
      return CreateTextDotSignatureUrlSave(
        JsonSerializer.Serialize<T>(
          value: typedObject, 
          options: null
        )
      );
    }

    public string? CreateTextDotSignatureUrlSave(string text)
    {
      return CreateTextDotSignatureUrlSave(Encoding.UTF8.GetBytes(text));
    }

    public string? CreateTextDotSignatureUrlSave(byte[] textBytes)
    {
      var textUrlEscaped = WebEncoders.Base64UrlTextEncoder.Encode(textBytes);

      var hashBytes = Algorithm.ComputeHash(textBytes);
      var hashTextUrlEscaped = WebEncoders.Base64UrlTextEncoder.Encode(hashBytes);

      // this is url save, string contains characters:
      // A-Z a-z 0-9 - _ = .
      return textUrlEscaped + "." + hashTextUrlEscaped;
    }

    public string? CreateTextDotSignatureUrlSave_FromBase64Url(string textUrlEscaped)
    {
      var textBytes = Encoding.UTF8.GetBytes(textUrlEscaped);
      var hashBytes = Algorithm.ComputeHash(textBytes);
      var hashTextUrlEscaped = WebEncoders.Base64UrlTextEncoder.Encode(hashBytes);

      // this is url save, string contains characters:
      // A-Z a-z 0-9 - _ = .
      return textUrlEscaped + "." + hashTextUrlEscaped;
    }

    public T? GetObjectIfVerified<T>(string textDotSignatureUrlSave)
    where T : class
    {
      return textDotSignatureUrlSave == null 
        ? null
        : JsonSerializer.Deserialize<T>(
        json: GetTextIfVerified(textDotSignatureUrlSave),
        options: null
      );
    }

    public string[]? GetDotStructureIfValid(string? text)
    {
      if(
        text == null 
        || string.IsNullOrWhiteSpace(text)
      ) return null;

      var messageComponents = text.Split(
        separator: '.', 
        count: 2, 
        options: StringSplitOptions.None
      );
      if(messageComponents.Length != 2) return null;
      else return messageComponents;
    }

    public string? GetTextIfVerified(string textDotSignatureUrlSave)
    {
      var messageComponents = GetDotStructureIfValid(textDotSignatureUrlSave);
      if(messageComponents == null) return null;

      string messageTextBase64UrlEncoded = messageComponents[0];
      string messageSignBase64UrlEncoded = messageComponents[1];

      return GetTextIfVerified(messageTextBase64UrlEncoded, messageSignBase64UrlEncoded);
    }

    public string? GetTextIfVerified(string textBase64Url, string signatureBase64Url)
    {
      byte[] messageTextBytes = WebEncoders.Base64UrlTextEncoder.Decode(textBase64Url);
      byte[] messageSignBytes = WebEncoders.Base64UrlTextEncoder.Decode(signatureBase64Url);

      return GetTextIfVerified(messageTextBytes, messageSignBytes);
    }

    public string? GetTextIfVerified(byte[] messageTextBytes, byte[] messageSignBytes)
    {
      byte[] textSignBytes = Algorithm.ComputeHash(messageTextBytes);
      if(textSignBytes.SequenceEqual(messageSignBytes) == false) return null;
      else return Encoding.UTF8.GetString(messageTextBytes);
    }
  }

}

#nullable restore