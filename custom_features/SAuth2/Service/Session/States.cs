#nullable enable

using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Http.Features;
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
using System.Diagnostics.CodeAnalysis;

namespace SAuth2.Service.Session.Api5.S
{



  public interface ICookieValueRequestContextState 
  {
    public class Undefined : ICookieValueRequestContextState {}
    public class Ok : ICookieValueRequestContextState
    { 
      public CookieName   CookieName = default!; 
      public CookiePolicy CookiePolicy = default!; 
      public bool         HttpContextIsHttps       = default!;
      public string?      HttpContextRequestOrigin = default!;
    }
  }


  public interface IStringCookieValueState 
  {
    public class Undefined : IStringCookieValueState {}
    public class NotEmpty : IStringCookieValueState 
    { 
      public string UrlSafeValue = default!; 
    }
    public class Empty  : IStringCookieValueState {  }
  }


  public interface IInterTypedCookieValueState<TData>
  {
    public class Undefined : IInterTypedCookieValueState<TData> {}
    public class Dots : IInterTypedCookieValueState<TData>
    { 
      public TData Instance = default!; 
    }
    
  }


  public interface ISignatureState<TData>
  {
    public class Undefined : ISignatureState<TData> {}
    public class Proper : ISignatureState<TData>
    { 
      public TData Data = default!; 
    }
    public class Invalid : ISignatureState<TData>
    {  
      public TData      Data   = default!; 
      public Exception? Errors = default!;
    }
  }




  public interface IExpireState<TData>
  {

    public class Undefined : IExpireState<TData> {}
    public class Valid : IExpireState<TData>
    { 
      public TData NotExpiredData = default!; 
      public DateTimeOffset Expires = default!;
    }
    public class Expired : IExpireState<TData>
    { 
      public TData ExpiredData = default!; 
      public DateTimeOffset ExpiredTime = default!;
    }
  }


  public interface ITypedCookieValueState<TData>
  {

    public class Undefined : ITypedCookieValueState<TData> {}
    public class Exists : ITypedCookieValueState<TData>
    {
      public TData Instance;
      public Exists(TData instance) {Instance = instance;}
    }
  }


  public interface ITypedSessionState<TData>
    where TData : notnull
  {

    public class Undefined : ITypedSessionState<TData> {}
    public class NoSessionData : ITypedSessionState<TData> 
    {
      public DateTimeOffset SynchronizationTime = DateTimeOffset.UtcNow;
    }
    public class SynchronizedValid : ITypedSessionState<TData>
    {
      public TData Instance = default!;
      public DateTimeOffset SynchronizationTime = DateTimeOffset.UtcNow;
    }
    public class ExistsExpired : ITypedSessionState<TData>
    {
      public TData Instance = default!;
    }
    public class Raw : ITypedSessionState<TData>
    {
      public TData Instance = default!;
    }
  }


  public interface IResponseCookiePolicyState 
  {
    public class Undefined : IResponseCookiePolicyState {}

  }
  
  
  public interface IResponseCookieState 
  {

    public class Undefined : IResponseCookieState {}
    public class Added : IResponseCookieState
    {
      public string Value = default!;
      public HttpContext HttpContext = default!;
    }
  }



}

#nullable restore