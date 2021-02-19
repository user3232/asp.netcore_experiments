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


  public static class StatefulApi
  {


    [return: NotNull]
    public static ICookieValueRequestContextState CheckRequestContext(
      CookiePolicyChecks checksService,
      ICookieValueRequestContextState state,
      HttpContext httpContextInput,
      CookiePolicy cookiePolicyInput,
      CookieName cookieNameInput
    )
    {
      if(state is ICookieValueRequestContextState.Ok) 
        return state;
      
      var secure = checksService.CookieInSecureContext(
        httpContextInput, 
        cookiePolicyInput.Secure
      );
      var (sameSiteOk, origin) = checksService.CookieFromSameSite(
        httpContextInput, 
        cookiePolicyInput.SameSite, 
        cookieNameInput.Domain
      );

      if(secure && sameSiteOk)
        return new ICookieValueRequestContextState.Ok()
        {
          CookiePolicy = cookiePolicyInput,
          CookieName = cookieNameInput,
          HttpContextIsHttps = httpContextInput.Request.IsHttps,
          HttpContextRequestOrigin = origin,
        };
      else
        return new ICookieValueRequestContextState.Undefined();
    }

    [return: NotNull]
    public static IStringCookieValueState ReadRequestCookie(
      CookieReaderWriter cookieAccessService,
      IStringCookieValueState state,
      ICookieValueRequestContextState requestContextInput,
      HttpContext httpContextInput, 
      CookieName cookieNameInput
    )
    {
      if(state is S.IStringCookieValueState.NotEmpty)
        return state;

      if(requestContextInput is ICookieValueRequestContextState.Ok policiesFulfilled)
      {
        var cookieValue = cookieAccessService.Read(httpContextInput, cookieNameInput.Name);
        if(string.IsNullOrWhiteSpace(cookieValue))
          return new IStringCookieValueState.Empty();
        else
          return new IStringCookieValueState.NotEmpty{ UrlSafeValue = cookieValue };
      }
      
      return state;
    }


    [return: NotNull]
    public static IResponseCookieState AddResponseCookie(
      CookieReaderWriter readerWriter,
      IStringCookieValueState state,
      HttpContext httpContext,
      CookieName cookieName,
      CookiePolicy cookiePolicy
    )
    {
      if(state is IStringCookieValueState.NotEmpty haveCookie)
      {
        var hc = readerWriter.AddResponseCookie(
          httpContext, 
          cookieName, 
          cookiePolicy, 
          haveCookie.UrlSafeValue
        );
        return new IResponseCookieState.Added{
          HttpContext = hc,
          Value = haveCookie.UrlSafeValue,
        };
      }
      else
      {
        return new IResponseCookieState.Undefined();
      }
    }


    [return: NotNull]
    public static IInterTypedCookieValueState<CookieKDESStrings> DeserializeToDots(
      Base64UrlKeyDataExpSigSerializer serializerService,
      IInterTypedCookieValueState<CookieKDESStrings> state,
      IStringCookieValueState stringCookieValueInput
    )
    {
      if(state is IInterTypedCookieValueState<CookieKDESStrings>.Dots)
        return state;
      
      if(stringCookieValueInput is IStringCookieValueState.NotEmpty cookie)
      {
        var kdes = serializerService.Deserialize(cookie.UrlSafeValue);
        if(kdes.Success == false) 
          return new IInterTypedCookieValueState<CookieKDESStrings>.Undefined();
        else 
          return new IInterTypedCookieValueState<CookieKDESStrings>.Dots
          {
            Instance = kdes.Data,
          };
      }
      
      return state;
    }


    [return: NotNull]
    public static IStringCookieValueState PartsSerialize(
      Base64UrlKeyDataExpSigSerializer serializer,
      IInterTypedCookieValueState<CookieKDESStrings> state
    )
    {
      if(state is IInterTypedCookieValueState<CookieKDESStrings>.Dots parts)
      {
        var maybeSerializationString = serializer.Serialize(parts.Instance);
        if(maybeSerializationString.Success == false) 
          return new IStringCookieValueState.Undefined();
        else 
          return new IStringCookieValueState.NotEmpty
          {
            UrlSafeValue = maybeSerializationString.Data,
          };
      }
      else
      {
        return new IStringCookieValueState.Undefined();
      }
    }


    [return: NotNull]
    public static ISignatureState<CookieKDESStrings> VerifyData(
      ICookieKDESStringsSigner signerService,
      ISignatureState<CookieKDESStrings> state,
      IInterTypedCookieValueState<CookieKDESStrings> dottedCookieValueInput
    )
    {
      if(state is ISignatureState<CookieKDESStrings>.Proper)
        return state;
      
      if(dottedCookieValueInput is IInterTypedCookieValueState<CookieKDESStrings>.Dots dottedCookieValue)
      {
        var verificationResult = signerService.Verify(dottedCookieValue.Instance);
        if(verificationResult.Success == false) 
          return new ISignatureState<CookieKDESStrings>.Invalid()
          {
            Data = verificationResult.Data
          };
        else 
          return new ISignatureState<CookieKDESStrings>.Proper()
          {
            Data = verificationResult.Data
          };
      }
      
      return state;
    }


    [return: NotNull]
    public static ISignatureState<CookieKDESStrings> SignData(
      ICookieKDESStringsSigner signerService,
      ISignatureState<CookieKDESStrings> st,
      IInterTypedCookieValueState<CookieKDESStrings> dottedCookieValueInput
    )
    {
      if(st is ISignatureState<CookieKDESStrings>.Proper)
        return st;

      if(dottedCookieValueInput is IInterTypedCookieValueState<CookieKDESStrings>.Dots parts)
      {
        var signResult = signerService.Sign(
          parts.Instance.Key,
          parts.Instance.Data,
          parts.Instance.Exp
        );

        if(signResult.Success == false) 
          return new ISignatureState<CookieKDESStrings>.Invalid()
          {
            Data = parts.Instance,
            Errors = signResult.MaybeErrors
          };
        else 
          return new ISignatureState<CookieKDESStrings>.Proper()
          {
            Data = signResult.Data
          };
      }
      
      return st;
    }


    [return: NotNull]
    public static ITypedCookieValueState<CookieKDES<TKey, TCookieData>> DeserializeToType<TKey, TCookieData>(
      CookieKDESSerializer<TKey, TCookieData> serializerService,
      ITypedCookieValueState<CookieKDES<TKey, TCookieData>> state,
      IInterTypedCookieValueState<CookieKDESStrings> interTypedCookieValueInput
    )
      where TKey : notnull
      where TCookieData : notnull
    {
      if(state is ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Exists)
        return state;
        
      if(interTypedCookieValueInput is IInterTypedCookieValueState<CookieKDESStrings>.Dots kdes)
      {
        var res = serializerService.MapDeserialize(kdes.Instance);
        if(res.Success == false)
          return new ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Undefined();
        else 
          return new ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Exists(
            res.Data
          );
      }
      
      return state;
    }


    [return: NotNull]
    public static IInterTypedCookieValueState<CookieKDESStrings> TypeSerializeToParts<TKey, TCookieData>(
      CookieKDESSerializer<TKey, TCookieData> typedSerializer,
      ITypedCookieValueState<CookieKDES<TKey, TCookieData>> state
    )
    where TKey : notnull
    where TCookieData : notnull
    {
      if(state is ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Exists kdes)
      {
        var res = typedSerializer.MapSerialize(kdes.Instance);
        if(res.Success == false)
          return new IInterTypedCookieValueState<CookieKDESStrings>.Undefined();
        else 
          return new IInterTypedCookieValueState<CookieKDESStrings>.Dots(){
            Instance = res.Data
          };
      }
      else
        return new IInterTypedCookieValueState<CookieKDESStrings>.Undefined();

      // return state switch
      // {
      //   HaveTypedInstance<KeyDataExpSig<TKey, TData>> kdes =>
      //     typedSerializer.MapSerialize(kdes.Instance) switch
      //     {
      //       Res<KeyDataExpSigStrings> {Success: true} res => 
      //         new HavePartsState<KeyDataExpSigStrings>(){
      //           Parts = res.Data
      //         },
      //       _ => new Undefined(),
      //     },
      //   _ => new Undefined()
      // };
    }


    [return: NotNull]
    public static IExpireState<CookieKDESStrings> ValidateExpire(
      IStringSerializer<DateTimeOffset> serializerService,
      IExpireState<CookieKDESStrings> state,
      IInterTypedCookieValueState<CookieKDESStrings> interTypedCookieValueInput
    )
    {
      if(state is IExpireState<CookieKDESStrings>.Valid)
        return state;
        
      if(interTypedCookieValueInput is IInterTypedCookieValueState<CookieKDESStrings>.Dots kdess)
      {
        var res = serializerService.StringDeserialize(kdess.Instance.Exp);
        if(res.Success && res.Data > DateTimeOffset.UtcNow)
        {
          return new IExpireState<CookieKDESStrings>.Valid()
          {
            Expires = res.Data,
            NotExpiredData = kdess.Instance,
          };
        } 
      }

      return state;
    }

    [return: NotNull]
    public static IExpireState<CookieKDES<TKey, TCookieData>> ValidateExpire<TKey, TCookieData>(
      IExpireState<CookieKDES<TKey, TCookieData>> state,
      ITypedCookieValueState<CookieKDES<TKey, TCookieData>> typedCookieValueInput
    )
      where TKey : notnull
      where TCookieData : notnull
    {
      if(state is IExpireState<CookieKDES<TKey, TCookieData>>.Valid)
        return state;
      
      if(typedCookieValueInput is ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Exists kdes)
        if(kdes.Instance.Exp > DateTimeOffset.UtcNow)
          return new IExpireState<CookieKDES<TKey, TCookieData>>.Valid()
          {
            Expires = kdes.Instance.Exp,
            NotExpiredData = kdes.Instance,
          };

      return state;
    }


    [return: NotNull]
    public static ITypedSessionState<SessionKDE<TKey, TSessionData>> RetriveSessionOnCookieKey<TCacheSrvc, TKey, TCookieData, TSessionData>(
      ISessionSrvc<TCacheSrvc, TKey, TSessionData> sessionService,
      ITypedSessionState<SessionKDE<TKey, TSessionData>> state,
      ITypedCookieValueState<CookieKDES<TKey, TCookieData>> typedCookieValueInput
    )
    where TKey   : notnull
    where TCookieData  : notnull
    where TSessionData : notnull
    {
      if(
        state is ITypedSessionState<SessionKDE<TKey, TSessionData>>.SynchronizedValid sv
        && typedCookieValueInput is ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Exists cv
        && sessionService.KeysEquall(cv.Instance.Key, sv.Instance.Key)
      )
        return state;
      
      if(typedCookieValueInput is ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Exists cookieValue)
      {
        var res = sessionService.Retrive(cookieValue.Instance.Key);
        if(res.HasValue == false)
          return new ITypedSessionState<SessionKDE<TKey, TSessionData>>.NoSessionData()
          {
            SynchronizationTime = DateTimeOffset.UtcNow,
          };
        else
          if(res.Value.exp > DateTimeOffset.UtcNow)
            return new ITypedSessionState<SessionKDE<TKey, TSessionData>>.SynchronizedValid()
            {
              Instance = new SessionKDE<TKey, TSessionData>()
              {
                Key = cookieValue.Instance.Key,
                Data = res.Value.maybeData,
                Exp = res.Value.exp,
              },
              SynchronizationTime = DateTimeOffset.UtcNow,
            };
          else
            return new ITypedSessionState<SessionKDE<TKey, TSessionData>>.ExistsExpired()
            {
              Instance = new SessionKDE<TKey, TSessionData>()
              {
                Key = cookieValue.Instance.Key,
                Data = res.Value.maybeData,
                Exp = res.Value.exp,
              }
            };
      }
      return state;
    }

    [return: NotNull]
    public static IExpireState<SessionKDE<TKey, TSessionData>> ValidateExpire<TKey, TSessionData>(
      IExpireState<SessionKDE<TKey, TSessionData>> state,
      ITypedSessionState<SessionKDE<TKey, TSessionData>> typedSessionInput
    )
      where TKey : notnull
      where TSessionData : notnull
    {
      if(state is not IExpireState<SessionKDE<TKey, TSessionData>>.Undefined)
        return state;

      if(typedSessionInput is ITypedSessionState<SessionKDE<TKey, TSessionData>>.SynchronizedValid kdes)
        if(kdes.Instance.Exp > DateTimeOffset.UtcNow)
          return new IExpireState<SessionKDE<TKey, TSessionData>>.Valid()
          {
            Expires = kdes.Instance.Exp,
            NotExpiredData = kdes.Instance,
          };

      return state;
    }



    [return: NotNull]
    public static ITypedSessionState<SessionKDE<TKey, TSessionData>> SynchronizeSession<TCacheSrvc, TKey, TSessionData>(
      ISessionSrvc<TCacheSrvc, TKey, TSessionData> sessionCacheSrvc,
      ITypedSessionState<SessionKDE<TKey, TSessionData>> state
    )
      where TKey : notnull
      where TSessionData : notnull
    {

      if(state is ITypedSessionState<SessionKDE<TKey, TSessionData>>.Raw rawSessionState)
      {
        if(rawSessionState.Instance.Exp > DateTimeOffset.UtcNow)
        {
          var res = sessionCacheSrvc.Synchronize(
            key:        rawSessionState.Instance.Key, 
            toSync:     rawSessionState.Instance.Data, 
            validUntil: rawSessionState.Instance.Exp
          );
          if(res.success) 
            return new ITypedSessionState<SessionKDE<TKey, TSessionData>>.SynchronizedValid()
            {
              Instance = new SessionKDE<TKey, TSessionData>()
              {
                Key  = rawSessionState.Instance.Key, 
                Data = rawSessionState.Instance.Data, 
                Exp  = rawSessionState.Instance.Exp
              }
            };
        }
      }
      
      return state;
    }

    




    
  }
  



}

#nullable restore