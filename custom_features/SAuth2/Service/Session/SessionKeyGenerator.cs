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
  
  public class SessionKeyGenerator 
  {
    public static int GuidDFormatLength => 36; 

    public string GenerateBase64Url() 
    {
      var guidBytes = new byte[16];
      RandomNumberGenerator.Fill(data: guidBytes);
      var sessionKey = new Guid(
        b: guidBytes              // A byte[16] array with initializes GUID
      )
      .ToString(format: "D");     // Series of lowercase hexadecimal 
                                  // digits in groups of 8, 4, 4, 4, and 12 digits 
                                  // and separated by hyphens.
                                  // (36 chars)
      return sessionKey;
    }
    public string? Load(string text)
      => Guid.TryParseExact(input: text,format: "D", out _) ? text : null;
    
    public bool IsConforming(string? text)
      => Guid.TryParseExact(input: text,format: "D", out _);
  }

}

#nullable restore