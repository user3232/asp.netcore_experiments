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

/*
  A. Signature trusted: 
    1. read cookie                                 HttpContext -> CookieValue base64Url String
    2. deserialize base64Url cookie to parts       base64Url String -> (key, data, expire, sig) strings
    3. filter inapriopriate expire                 expire -> DateTimeOffset -> isExpired boolean
    4. verify signature                            sig -> (key,data,expire) -> verified: key,data,expire
    5. deserialize to types                        (key,data,expire) -> TKey,TData,DateTimeOffset
    6. get server data                             TKey -> (TServerData, DateTimeOffest (serv expire)) 
  B. Signed but with revocation:
    1. read cookie
    2. deserialize base64Url to parts
    3. verify signature
    4. deserialize to types
    5. get server data
    6. filter inapriopriate expire based on server data 
  C. Unsigned:
    1. read cookie
    2. deserialize to key data
    3. deserialize to types
    4. get server data
    5. filter server data expire
  D. Unsigned without server data
    1. read cookie
    2. deserialize base64Url cookie value
    3. deserialize data to type
    4. get key from type
    5. check key in (key, expire) database
*/

namespace SAuth2.Service.Session.Api5
{

  public class KeyCookieSession<TKey, TCookieData, TSessionData>
    where TKey : notnull
    where TCookieData : notnull
    where TSessionData : notnull
  {
    public TKey           Key         = default!;
    public TCookieData?   CookieData  = default!;
    public DateTimeOffset CookieExp   = default!;
    public TSessionData?   SessionData = default!;
    public DateTimeOffset SessionExp  = default!;
  }


  public class SignedSessonFeature<TKey, TCookieData, TSessionData, TSessionSrvc>
    where TKey         : notnull
    where TCookieData  : notnull
    where TSessionData : notnull
    where TSessionSrvc : notnull
  {
    public CookiePolicy                                   cookiePolicy       = default!;
    public CookieName                                     cookieName         = default!;

    public CookiePolicyChecks                             cookiePolicyChecks = default!;
    public CookieReaderWriter                             cookieReaderWriter = default!;
    public Base64UrlKeyDataExpSigSerializer               kdessSerializer = default!;
    public CookieKDESSerializer<TKey, TCookieData>        typedSerializer = default!;
    public ISessionSrvc<TSessionSrvc, TKey, TSessionData> sessionSrvc = default!;
    public ICookieKDESStringsSigner                       signerService = default!;

    public IStringSerializer<DateTimeOffset>              dateTimeOffsetSerializer = default!;


    public class KCSSessionRecreateOptions
    {
      public bool ValidateCookieSignature = true;
      public bool ValidateCookieExpire    = true;
      public bool ValidateSessionExpire   = true;
    }

    

    public KeyCookieSession<TKey, TCookieData, TSessionData>? RecreateCookieAndSession(
      KCSSessionRecreateOptions options,
      HttpContext httpContext
    )
    {
      S.ICookieValueRequestContextState requestContextState = SS.CheckRequestContext(
        checksService:     cookiePolicyChecks,
        state:             new S.ICookieValueRequestContextState.Undefined(),
        httpContextInput:  httpContext,
        cookiePolicyInput: cookiePolicy,
        cookieNameInput:   cookieName
      );
      if(requestContextState is not S.ICookieValueRequestContextState.Ok)
        return null;

      var stringCookieValueState = SS.ReadRequestCookie(
        cookieAccessService: cookieReaderWriter,
        state: new S.IStringCookieValueState.Undefined(),
        requestContextInput: requestContextState,
        httpContextInput: httpContext,
        cookieNameInput: cookieName
      );
      if(stringCookieValueState is not S.IStringCookieValueState.NotEmpty) 
        return null;
      
      var dottedCookieValueState = SS.DeserializeToDots(
        serializerService: kdessSerializer,
        state: new S.IInterTypedCookieValueState<CookieKDESStrings>.Undefined(),
        stringCookieValueInput: stringCookieValueState
      );


      var signedCookieValueState = SS.VerifyData(
        signerService: signerService,
        state: new S.ISignatureState<CookieKDESStrings>.Undefined(),
        dottedCookieValueInput: dottedCookieValueState
      );


      var dottedCookieValueExpireState = SS.ValidateExpire(
        serializerService: dateTimeOffsetSerializer,
        state: new S.IExpireState<CookieKDESStrings>.Undefined(),
        interTypedCookieValueInput: dottedCookieValueState
      );


      var typedCookieValueState = SS.DeserializeToType(
        serializerService: typedSerializer,
        state: new S.ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Undefined(),
        interTypedCookieValueInput: dottedCookieValueState
      );


      var typedCookieValueExpireState = SS.ValidateExpire(
        state: new S.IExpireState<CookieKDES<TKey, TCookieData>>.Undefined(),
        typedCookieValueInput: typedCookieValueState
      );


      var typedSessionState = SS.RetriveSessionOnCookieKey(
        sessionService: sessionSrvc,
        state: new S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.Undefined(),
        typedCookieValueInput: typedCookieValueState
      );


      // options should deal with valid substates combinations
      // and output building possibilities
      // what decides validity: server or cookie?
      // destroy invalid cookie session?
      // suppose cookie/server data are invalid, what about key?
      //   key have no associated data
      //   key can be reused or new key can be created

      if(
        typedCookieValueState is S.ITypedCookieValueState<CookieKDES<TKey, TCookieData>>.Exists cv
      )
      {
        if(
          options.ValidateCookieExpire == false
        )
        {
          if(
            options.ValidateSessionExpire == false
          )
          {
            if(
              typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.SynchronizedValid sv
            )
              return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                Key = cv.Instance.Key,
                CookieData = cv.Instance.Data,
                CookieExp = cv.Instance.Exp,
                SessionData = sv.Instance.Data,
                SessionExp = sv.Instance.Exp,
              };
            else if(
              typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.ExistsExpired ee
            )
              return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                Key = cv.Instance.Key,
                CookieData = cv.Instance.Data,
                CookieExp = cv.Instance.Exp,
                SessionData = ee.Instance.Data,
                SessionExp = ee.Instance.Exp,
              };
            else if(
              typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.NoSessionData
              || typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.Raw
              || typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.Undefined
            )
              return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                Key = cv.Instance.Key,
                CookieData = cv.Instance.Data,
                CookieExp = cv.Instance.Exp,
                SessionData = default,
                SessionExp = DateTimeOffset.MinValue,
              };
            else
              return null;
          }
          else
          {
            if(
              typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.SynchronizedValid sv
            )
              return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                Key = cv.Instance.Key,
                CookieData = cv.Instance.Data,
                CookieExp = cv.Instance.Exp,
                SessionData = sv.Instance.Data,
                SessionExp = sv.Instance.Exp,
              };
            else
              return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                Key = cv.Instance.Key,
                CookieData = cv.Instance.Data,
                CookieExp = cv.Instance.Exp,
                SessionData = default,
                SessionExp = DateTimeOffset.MinValue,
              };
          }
        }
        else
        {
          if(
            typedCookieValueExpireState is S.IExpireState<CookieKDES<TKey, TCookieData>>.Valid
          )
          {
            if(
              options.ValidateSessionExpire == false
            )
            {
              if(
                typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.SynchronizedValid sv
              )
                return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                  Key = cv.Instance.Key,
                  CookieData = cv.Instance.Data,
                  CookieExp = cv.Instance.Exp,
                  SessionData = sv.Instance.Data,
                  SessionExp = sv.Instance.Exp,
                };
              else if(
                typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.ExistsExpired ee
              )
                return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                  Key = cv.Instance.Key,
                  CookieData = cv.Instance.Data,
                  CookieExp = cv.Instance.Exp,
                  SessionData = ee.Instance.Data,
                  SessionExp = ee.Instance.Exp,
                };
              else if(
                typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.NoSessionData
                || typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.Raw
                || typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.Undefined
              )
                return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                  Key = cv.Instance.Key,
                  CookieData = cv.Instance.Data,
                  CookieExp = cv.Instance.Exp,
                  SessionData = default,
                  SessionExp = DateTimeOffset.MinValue,
                };
              else
                return null;
            }
            else
            {
              if(
                typedSessionState is S.ITypedSessionState<SessionKDE<TKey, TSessionData>>.SynchronizedValid sv
              )
                return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                  Key = cv.Instance.Key,
                  CookieData = cv.Instance.Data,
                  CookieExp = cv.Instance.Exp,
                  SessionData = sv.Instance.Data,
                  SessionExp = sv.Instance.Exp,
                };
              else
                return new KeyCookieSession<TKey, TCookieData, TSessionData>(){
                  Key = cv.Instance.Key,
                  CookieData = cv.Instance.Data,
                  CookieExp = cv.Instance.Exp,
                  SessionData = default,
                  SessionExp = DateTimeOffset.MinValue,
                };
            }
          }
          else
          {
            return null;
          }
        }
      }
      else
      {
        return null;
      }
      
      
      
    }

    public SignedSessonFeature<TKey, TCookieData, TSessionData, TSessionSrvc> NewWithUniqueSessionKey()
    {
      return null!;
    }

    public SignedSessonFeature<TKey, TCookieData, TSessionData, TSessionSrvc> Empty()
    {
      
      return null!;
    }
    

    public HttpContext ToCookieValue(HttpContext httpContext)
    {

      return httpContext;
    }

    public TSessionData ToServerStore()
    {

      return default!;
    }




    public TKey GetBrowserKey()
    {
      return default!;
    }

    public TCookieData GetCookieData()
    {

      return default!;
    }

    public TSessionData GetServerData()
    {

      return default!;
    }

  }

}

#nullable restore