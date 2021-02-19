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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting.Server.Features;

using WebEncoders = Microsoft.AspNetCore.WebUtilities;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Diagnostics.CodeAnalysis;
using SS = SAuth2.Service.Session.Api5.S.StatefulApi;



namespace SAuth2.Service.Session.Api5
{

  #region Interfaces

  public struct Res<T> 
    where T : notnull
  {
    public T? MaybeData; 
    public Exception? MaybeErrors;
    // [MemberNotNullWhen(true, nameof(MaybeData))]
    public bool Success => MaybeData != null;
    
    public T Data => MaybeData != null 
      ? MaybeData 
      : throw new InvalidOperationException();
    
    // public struct Res<T> 
    // {
    //   public struct Box {public T Unbox;}
    //   public Box? MaybeData; 
    //   public Exception? MaybeErrors;
    //   public bool Success => MaybeData.HasValue;
    //   public T Data => MaybeData.HasValue 
    //     ? MaybeData.Value.Unbox 
    //     : throw new InvalidOperationException();
    // }
  }
  
  public static class Res
  {
    public static Res<T> Success<T>(T data) where T : notnull
      => new Res<T>(){
        MaybeData = data, 
        MaybeErrors = null,
      };
    
    public static Res<T> Fail<T>(Exception? e = null) where T : notnull
      => new Res<T>(){
        MaybeData = default, 
        MaybeErrors = e,
      };

    // public static class Res
    // {
    //   public static Res<T> Success<T>(T data) 
    //     => new Res<T>(){
    //       MaybeData = new Res<T>.Box(){Unbox = data}, 
    //       MaybeErrors = null,
    //     };
      
    //   public static Res<T> Fail<T>(Exception? e = null) 
    //     => new Res<T>(){
    //       MaybeData = null, 
    //       MaybeErrors = e,
    //     };
    // }
  }
  
  

  public interface ICloneable<T>
    where T : notnull
  {
    Res<T> Clone();
  }

  public interface IStringSerializer<T>
    where T : notnull
  {
    Res<T> StringDeserialize(string stringedInstance);

    Res<string> StringSerialize(T instance);
  }

  public interface IJsonStringSerializerMixin<T> : IStringSerializer<T>
    where T : notnull
  {
    Res<T> IStringSerializer<T>.StringDeserialize(
      string stringedInstance
    )
    {
      try { return Res.Success(JsonSerializer.Deserialize<T>(stringedInstance)); }
      catch(Exception e) { return Res.Fail<T>(e); }
    }

    Res<string> IStringSerializer<T>.StringSerialize(T instance) 
    {
      try { return Res.Success(JsonSerializer.Serialize<T>(instance)); }
      catch(Exception e) { return Res.Fail<string>(e); }
    }
  }

  public interface IDottedSerializerMixin
  {
    Res<string> SerializeDotsSeparated(params string[] parts)
    {
      static string Encode(string x) 
        => WebEncoders.Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(x));

      try
      {
        if(parts.Length < 1) 
          return Res.Fail<string>(
            new ArgumentException(
              "At least one empty string is needed.", 
              nameof(parts)));
        
        if(parts.Length == 1) 
          return Res.Success(Encode(parts[0]));

        return Res.Success(string.Join('.', parts.Select(Encode)));
      }
      catch(Exception e) { return Res.Fail<string>(e); }
    }
    
    Res<string[]> DeserializeDotsSeparated(
      int numberOfDotSeparatedElements, 
      string? dottedBase64UrlData
    )
    {
      static string Decode(string x) 
        => Encoding.UTF8.GetString(WebEncoders.Base64UrlTextEncoder.Decode(x));
      
      try
      {
        if(numberOfDotSeparatedElements < 2) 
          return Res.Fail<string[]>(
            new ArgumentException(
              "At least 2 elements must be expected.", 
              nameof(numberOfDotSeparatedElements)));
        
        if(string.IsNullOrWhiteSpace(dottedBase64UrlData)) 
          return Res.Fail<string[]>(
            new ArgumentException(
              "Provided string is empty or white space", 
              nameof(dottedBase64UrlData)));
        
        var dotSplitted = dottedBase64UrlData.Split('.', numberOfDotSeparatedElements);
        if(dotSplitted.Length != numberOfDotSeparatedElements) 
          return Res.Fail<string[]>(
            new ArgumentException(
              $"Provided string should have {numberOfDotSeparatedElements}"
              + " dot separated elements, instead it have {dotSplitted.Length}.", 
              nameof(dottedBase64UrlData)));
        
        return Res.Success(dotSplitted.Select(Decode).ToArray());
      }
      catch(Exception e) { return Res.Fail<string[]>(e); }
    }

    Res<string[]> DeserializeDotsSeparated(string? dottedBase64UrlData)
    {
      static string Decode(string x) 
        => Encoding.UTF8.GetString(WebEncoders.Base64UrlTextEncoder.Decode(x));
      
      try
      {
        var dotSplitted = dottedBase64UrlData!.Split('.');
        return Res.Success(dotSplitted.Select(Decode).ToArray());
      }
      catch(Exception e) { return Res.Fail<string[]>(e); }
    }
  }

  public interface IJsonBase64UrlSerializerMixin<T> 
    where T : notnull
  {
    Res<string> SerializeJsonBase64Url(T obj)
    {
      try
      {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes<T>(obj);
        return Res.Success(WebEncoders.Base64UrlTextEncoder.Encode(jsonBytes));
      }
      catch(Exception e) { return Res.Fail<string>(e); }
    }
    Res<T> DeserializeJsonBase64Url(string jsonBase64Url)
    {
      try
      {
        var jsonBytes = WebEncoders.Base64UrlTextEncoder.Decode(jsonBase64Url);
        return Res.Success(JsonSerializer.Deserialize<T>(jsonBytes));
      }
      catch(Exception e) { return Res.Fail<T>(e); }
    }
  }

  public interface IMapSerializer<TSource, TTarget>
    where TSource : notnull
    where TTarget : notnull
  {
    Res<TTarget> MapSerialize(TSource source);
    Res<TSource> MapDeserialize(TTarget target);
  }

  public interface IBase64UrlSerializer<T>
  where T: notnull
  {
    Res<string> Serialize(T instance);
    Res<T> Deserialize(string base64UrlSerializedInstance);
  }

  public interface ICookieKDESStringsSigner
  {
    Res<CookieKDESStrings> Sign(string key, string data, string expire);
    Res<CookieKDESStrings> Verify(CookieKDESStrings KeyDataExpSigStrings);
  }

  public interface ISessionSrvc<TCacheSrvc, TKey, TServerData>
  where TKey : notnull
  where TServerData : notnull
  {
    TServerData CreateEmpty();
    (bool success, TCacheSrvc srvc) Synchronize(TKey key, TServerData? toSync, DateTimeOffset validUntil);
    (TServerData? maybeData, DateTimeOffset exp)? Retrive(TKey key);
    bool KeysEquall(TKey left, TKey right);
  }

  public interface IGenerateEmpty<T>
  {
    T Empty();
  }

  public interface ISessionKeyGenerator<TKey> 
  {
    TKey GenerateUnique();
  }

  #endregion


  #region Implementations

  public class CookieReaderWriter
  {
    
    public string Read(HttpContext httpContext, string cookieName)
      => httpContext.Request.Cookies[cookieName];
    

    public HttpContext AddResponseCookie(
      HttpContext httpContext, 
      CookieName cookieName,
      CookiePolicy cookiePolicy,
      string? cookieValue
    )
    {
      if(cookieValue == null) 
        return httpContext;
      
      var domain = httpContext.Request.Host.Value;
      httpContext.Response.Cookies.Append(
        key: cookieName.Name,
        value: cookieValue,
        options: new CookieOptions{
          Domain   = cookieName.Domain,
          Path     = cookieName.Path,
          Secure   = cookiePolicy.Secure,
          HttpOnly = cookiePolicy.HttpOnly,
          SameSite = cookiePolicy.SameSite,
          Expires  = cookiePolicy.Expires, // "session" cookies should not specify
          MaxAge   = cookiePolicy.MaxAge,  // "session" cookies should not specify
        }
      );
      return httpContext;
    }
    
  }

  public class Base64UrlKeyDataExpSigSerializer
    : IDottedSerializerMixin, IBase64UrlSerializer<CookieKDESStrings>
  {

    public Res<CookieKDESStrings> Deserialize(string base64UrlSerializedInstance)
    {
      var ds = this as IDottedSerializerMixin;
      var parts = ds.DeserializeDotsSeparated(4, base64UrlSerializedInstance);
      if(parts.Success == false) 
        return Res.Fail<CookieKDESStrings>(parts.MaybeErrors);
      var data = new CookieKDESStrings() {
        Key  = parts.Data[0],
        Data = parts.Data[1],
        Exp  = parts.Data[2],
        Sig  = parts.Data[3],
      };
      return Res.Success(data);
    }

    public Res<string> Serialize(CookieKDESStrings instance)
    {
      var ds = this as IDottedSerializerMixin;
      return ds.SerializeDotsSeparated(instance.Key, instance.Data, instance.Exp, instance.Sig);
    }
  }

  public class JsonSerializer<T> : IJsonStringSerializerMixin<T> where T : notnull {}

  public class DateTimeOffsetUnixSecsSerializer : IStringSerializer<DateTimeOffset>
  {
    Res<DateTimeOffset> IStringSerializer<DateTimeOffset>.StringDeserialize(
      string stringedInstance
    )
    {
      try
      {
        return Res.Success(
          DateTimeOffset.FromUnixTimeSeconds(
            BitConverter.ToInt64(
              Encoding.UTF8.GetBytes(stringedInstance))));
      }
      catch(Exception e) { return Res.Fail<DateTimeOffset>(e); }
    }

    Res<string> IStringSerializer<DateTimeOffset>.StringSerialize(
      DateTimeOffset instance
    )
    {
      try
      {
        return Res.Success(
          Encoding.UTF8.GetString(
            BitConverter.GetBytes(
              instance.ToUnixTimeSeconds()))
        );
      }
      catch(Exception e) { return Res.Fail<string>(e); }
    } 
  }

  public class StringSerializer : IStringSerializer<string>
  {
    Res<string> IStringSerializer<string>.StringDeserialize(
      string stringedInstance
    ) => Res.Success(stringedInstance);

    Res<string> IStringSerializer<string>.StringSerialize(
      string instance
    ) => Res.Success(instance);
  }

  public class CookieKDESSerializer<TKey, TCookieData> :
    IMapSerializer<CookieKDES<TKey, TCookieData>, CookieKDESStrings>
    where TKey : notnull
    where TCookieData : notnull
  {
    public IStringSerializer<TKey> keySerializer = default!;
    public IStringSerializer<TCookieData> dataSerializer = default!;
    public IStringSerializer<DateTimeOffset> timeSerializer = default!;
    public IStringSerializer<string> signatureSerializer = default!;


    public Res<CookieKDES<TKey, TCookieData>> MapDeserialize(CookieKDESStrings target)
    {
      var keyRes = keySerializer.StringDeserialize(target.Key);
      if(keyRes.Success == false) 
        return Res.Fail<CookieKDES<TKey, TCookieData>>(keyRes.MaybeErrors);
      
      var dataRes = dataSerializer.StringDeserialize(target.Data);
      if(dataRes.Success == false) 
        return Res.Fail<CookieKDES<TKey, TCookieData>>(dataRes.MaybeErrors);
      
      var timeRes = timeSerializer.StringDeserialize(target.Exp);
      if(timeRes.Success == false) 
        return Res.Fail<CookieKDES<TKey, TCookieData>>(timeRes.MaybeErrors);
      
      var signatureRes = signatureSerializer.StringDeserialize(target.Exp);
      if(signatureRes.Success == false) 
        return Res.Fail<CookieKDES<TKey, TCookieData>>(signatureRes.MaybeErrors);
      
      return Res.Success(new CookieKDES<TKey, TCookieData>() {
        Key  = keyRes.Data,
        Data = dataRes.Data,
        Exp  = timeRes.Data,
        Sig  = signatureRes.Data,
      });
    }

    public Res<CookieKDESStrings> MapSerialize(CookieKDES<TKey, TCookieData> source)
    {
      var key = keySerializer.StringSerialize(source.Key);
      if(key.Success == false) return Res.Fail<CookieKDESStrings>();
      var data = source.Data == null ? Res.Success("") : dataSerializer.StringSerialize(source.Data);
      if(data.Success == false) return Res.Fail<CookieKDESStrings>();
      var exp = timeSerializer.StringSerialize(source.Exp);
      if(exp.Success == false) return Res.Fail<CookieKDESStrings>();
      var sig = signatureSerializer.StringSerialize(source.Sig);
      if(sig.Success == false) return Res.Fail<CookieKDESStrings>();

      return Res.Success(new CookieKDESStrings() {
        Key = key.Data,
        Data = data.Data,
        Exp = exp.Data,
        Sig = sig.Data,
      });
    }

  }

  public class SessionKDE<TKey, TSessionData>
    where TKey : notnull
    where TSessionData : notnull
  {
    public TKey           Key  = default!;
    public TSessionData?  Data = default!;
    public DateTimeOffset Exp = default!;
  }

  public class CookieKDES<TKey, TCookieData>
  where TKey : notnull
  where TCookieData : notnull
  {
    public TKey           Key  = default!;
    public TCookieData?         Data = default!;
    public DateTimeOffset Exp  = default!;
    public string         Sig  = default!;
  }

  public class CookieKDESStrings
  {
    public string Key  = "";
    public string Data = "";
    public string Exp  = "";
    public string Sig  = "";
  }

  public class CookieKDESStringsSigner : ICookieKDESStringsSigner
  {
    public HMACSHA256 Algorithm;
    public CookieKDESStringsSigner() { Algorithm = new HMACSHA256(); }
    public CookieKDESStringsSigner(byte[] signingPrivateKey) 
    { 
      Algorithm = new HMACSHA256(signingPrivateKey); 
    }


    public Res<CookieKDESStrings> Sign(string key, string data, string expire)
    {
      var bytes = Encoding.UTF8.GetBytes(key + data + expire);
      var signatureBytes = new byte[256];
      var signSuccess = Algorithm.TryComputeHash(bytes, signatureBytes, out var bytesWritten);
      
      return signSuccess 
        ? Res.Success(new CookieKDESStrings() {
          Key = key,
          Data = data,
          Exp = expire,
          Sig = Encoding.UTF8.GetString(signatureBytes.AsSpan(0, bytesWritten)),
        })
        : Res.Fail<CookieKDESStrings>();
    }

    public Res<CookieKDESStrings> Verify(CookieKDESStrings KeyDataExpSigStrings)
    {
      var signed = Sign(
        KeyDataExpSigStrings.Key, 
        KeyDataExpSigStrings.Data, 
        KeyDataExpSigStrings.Exp
      );
      
      return signed.Success && signed.Data.Sig == KeyDataExpSigStrings.Sig 
        ? Res.Success(KeyDataExpSigStrings) 
        : Res.Fail<CookieKDESStrings>();
    }

  }

  public class CookiePolicyChecks
  {

    public bool CookieInSecureContext(
      HttpContext httpContext, 
      bool secure
    )
      => secure == false ? true : httpContext.Request.IsHttps;

    public (bool, string?) CookieFromSameSite(
      HttpContext httpContext, 
      SameSiteMode sameSite, 
      string domain
    )
    {
      var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();
      var res = sameSite switch 
      {
        SameSiteMode.Strict => origin == domain,
        SameSiteMode.Lax    => string.IsNullOrWhiteSpace(origin) || origin == domain,
        _                   => true,
      };
      return (res, origin);
    }

    // public S.IStateful StatefulRequestPolicyConformance(
    //   HttpContext httpContext,
    //   CookiePolicy cookiePolicy,
    //   CookieName cookieName
    // )
    // {
    //   var secure = CookieInSecureContext(httpContext, cookiePolicy.Secure);
    //   var (sameSiteOk, origin) = CookieFromSameSite(
    //     httpContext, 
    //     cookiePolicy.SameSite, 
    //     cookieName.Domain
    //   );

    //   if(secure && sameSiteOk)
    //     return new S.RequestCookiePolicyFulfilled()
    //     {
    //       IsHttps = httpContext.Request.IsHttps,
    //       RequestOrigin = origin,
    //     };
    //   else
    //     return new S.RequestCookiePolicyFailed();
    // }

  }

  public class MemorySessionCacheSrvc<TKey, TServerData, TKeySrvc, TServerDataGen>
    : ISessionSrvc<MemorySessionCacheSrvc<TKey, TServerData, TKeySrvc, TServerDataGen>, TKey, TServerData>
    where TKey : notnull
    where TServerData : notnull
    where TKeySrvc : IEqualityComparer<TKey>
    where TServerDataGen : IGenerateEmpty<TServerData>
  {
    public TKeySrvc KeySrvc = default!;
    public TServerDataGen CacheFac = default!;
    public Dictionary<TKey, (TServerData? maybeData, DateTimeOffset exp)> Store = default!;

    public MemorySessionCacheSrvc() 
    {
      Store = new Dictionary<TKey, (TServerData? data, DateTimeOffset exp)>(KeySrvc);
    }

    public bool KeysEquall(TKey left, TKey right) => KeySrvc.Equals(left, right);
    
    public TServerData CreateEmpty() => CacheFac.Empty();

    public (TServerData? maybeData, DateTimeOffset exp)? Retrive(TKey key)
    {
      var success = Store.TryGetValue(key, out var value);
      // key dont exist or data is null => return nothing :
      if(success == false) return null;

      var (data, time) = value;
      // key exists, time is valid   => return data and time
      if(time > DateTimeOffset.UtcNow)
      {
        return value;
      }
      // key exists, time is invalid => remove stale data from store
      //                                and return it.
      else
      {
        Store.Remove(key, out var staleValue);
        return staleValue;
      }
    }


    public (bool success, MemorySessionCacheSrvc<TKey, TServerData, TKeySrvc, TServerDataGen> srvc) Synchronize(
      TKey key, TServerData? toSync, DateTimeOffset validUntil
    )
    {
      // store if valid time:
      // (null data is ok)
      if(validUntil > DateTimeOffset.UtcNow)
      {
        Store[key] = (toSync, validUntil);
        return (success: false, srvc: this);
      }
      // we possibli mutate internal state :(
      return (success: true, srvc: this);
    }

    
    
  }

  #endregion




  #region PODs

  public class GuidKeyGen 
    : ISessionKeyGenerator<string>, 
      IEqualityComparer<string>
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
                                  // (36 chars from set [abcdef0123456789])
      return sessionKey;
    }
    public string? Verify(string text)
      => Guid.TryParseExact(input: text,format: "D", out _) ? text : null;

    public string GenerateUnique() => GenerateBase64Url();

    public bool Equals([AllowNull] string x, [AllowNull] string y)
    {
      if(x == null || y == null) return false;
      else return x == y;
    }

    public int GetHashCode([DisallowNull] string obj) => obj.GetHashCode();
  }

  public class CookieName
  {
    public string  Domain  = "localhost";
                          // "example.com";     // /etc/hosts and custom cert
                          // "sub.example.com"; // /etc/hosts and custom cert
    public string  Path    = "/";
    public string  Name    = "session";
  }
  
  
  public class CookiePolicy
  {
    public bool            Secure      = false;  
    public bool            HttpOnly    = true; 
    public SameSiteMode    SameSite    = SameSiteMode.Lax; 
    public DateTimeOffset? Expires     = null; // "session-cookies" don't specify
    public TimeSpan?       MaxAge      = null; // "session-cookies" don't specify
  }

  public class SessionApiPolicy
  {
    public TimeSpan? CookieRefreshSpan = null; // 
    public TimeSpan? CacheRefreshSpan  = TimeSpan.FromMinutes(20);


    public bool IgnoreNotSigned  = true;
    public bool CheckKeyInCache  = false;
    public bool DiscardExpired   = true;
    public bool IsEssential      = false;  // what to do when user dont agree for 
                                           // cookies? Cookie middleware can:
                                           // 1. continue service without providing
                                           //    feature.. or
                                           // 2. terminate request printing with
                                           //    some error message
  }

  public class CacheSrvcPolicy
  {
  }
  
  #endregion

}
#nullable restore