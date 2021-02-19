using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;       // Task
using Microsoft.AspNetCore.Http;    // HttpContext
using Microsoft.AspNetCore.Routing; // IEndpointRouteBuilder
                                    // HttpContex.GetRouteData
using System.Security.Claims;       // ClaimsIdentity
using System.Security.Principal;    // IIdentity
using System.IO;                    // StringReader
using Microsoft.Extensions.Primitives; //StringSegment
using Microsoft.Net.Http.Headers;   // NameValueHeaderValue
using System.Reflection;            // BindingFlags.Public | BindingFlags.Static

using SAuth2.Extensions;            // Indent

namespace SAuth2.Endpoints
{
  
  public static class ReqResInfo
  {
    /// <summary>
    /// Displays various informations about http request/response
    /// and request/response metadata associated by the server.
    /// </summary>
    /// <param name="httpReqResp">>Http request data mapped to objects</param>
    /// <returns>Nothing, but response is mutated as side effect.</returns>
    public static async Task Print(HttpContext httpReqResp)
    {
      async Task Print(string s) => await httpReqResp.Response.WriteAsync(s);
      async Task PrintLine(string s) => await Print(s+ "\n");

      await Print("Hellow World!");

      await PrintLine("Connection:");

        await PrintLine("ClientCertificate:".Indent());
          await PrintLine(httpReqResp.Connection.ClientCertificate?.ToString().Indent(2));
        
        await Print("Id: ".Indent());
        await PrintLine(httpReqResp.Connection.Id?.ToString());
        
        await Print("LocalIpAddress: ".Indent());
        await PrintLine(httpReqResp.Connection.LocalIpAddress?.ToString());
        
        await Print("LocalPort: ".Indent());
        await PrintLine(httpReqResp.Connection.LocalPort.ToString());
        
        await Print("RemoteIpAddress: ".Indent());
        await PrintLine(httpReqResp.Connection.RemoteIpAddress?.ToString());
        
        await Print("RemotePort: ".Indent());
        await PrintLine(httpReqResp.Connection.RemotePort.ToString());

      await PrintLine("Features:");
      foreach (var feature in httpReqResp.Features)
      {
        await PrintLine(feature.Key.FullName.Indent());
      }

      await PrintLine("GetEndpoint():");
        await PrintLine(
          "DisplayName: ".Indent() 
          + httpReqResp.GetEndpoint().DisplayName?.ToString()
        );
        await PrintLine(
          "RequestDelegate: ".Indent() 
          + httpReqResp.GetEndpoint().RequestDelegate?.ToString()
        );
        await PrintLine("Metadata: ".Indent());
        foreach (var meta in httpReqResp.GetEndpoint().Metadata)
        {
          await PrintLine(meta?.ToString().Indent(2));
        }

      await PrintLine("GetRouteData():");
        await PrintLine("DataTokens:".Indent());
        foreach (var token in httpReqResp.GetRouteData().DataTokens)
        {
          await PrintLine(token.Key?.ToString().Indent(2));
        }
        await PrintLine("Routers:".Indent());
        foreach (var iRouter in httpReqResp.GetRouteData().Routers)
        {
          await PrintLine(iRouter?.ToString().Indent(2));
        }
        await PrintLine("Values:".Indent());
        foreach (var value in httpReqResp.GetRouteData().Values)
        {
          await PrintLine($"{value.Key}: {value.Value}".Indent(2));
        }

      await PrintLine("GetServerVariable(????):");

      await PrintLine("Items: (container for associating data with this request.)");
        foreach (var item in httpReqResp.Items)
        {
          await PrintLine($"{item.Key}: {item.Value}".Indent());
        }

      
      await PrintLine("User: (ClaimsPrincipal)");
        await PrintLine("Identity: (Primary IIdentity)".Indent());
          await PrintLine(
            $"AuthenticationType: {httpReqResp.User.Identity.AuthenticationType}"
            .Indent(2)
          );
          await PrintLine(
            $"IsAuthenticated: {httpReqResp.User.Identity.IsAuthenticated}"
            .Indent(2)
          );
          await PrintLine(
            $"Name: {httpReqResp.User.Identity.Name}"
            .Indent(2)
          );
        await PrintLine("Identities: ([ClaimsIdentity])".Indent());
          foreach (ClaimsIdentity ident in httpReqResp.User.Identities)
          {
            await PrintLine(
              $"Name: {ident.Name}"
              .Indent(2)
            );
          }

      await PrintLine("WebSockets: (WebSocketManager)");
        await PrintLine(
          $"IsWebSocketRequest: {httpReqResp.WebSockets.IsWebSocketRequest}"
          .Indent()
        );
        await PrintLine(
          $"WebSocketRequestedProtocols:"
          .Indent()
        );
        foreach (string protocol in httpReqResp.WebSockets.WebSocketRequestedProtocols)
        {
          await PrintLine(protocol.Indent(2));
        }

      // session must be enabled, otherwise it throws
      /* await PrintLine("Session: (ISession)");
        // A unique identifier for the current session. This is not the
        // same as the session cookie since the cookie lifetime may not
        // be the same as the session entry lifetime in the data store.
        await PrintLine(
          $"Id: {httpReq.Session?.Id}"
          .Indent()
        );
        // Indicate whether the current session has loaded.
        await PrintLine(
          $"IsAvailable: {httpReq.Session?.IsAvailable}"
          .Indent()
        );
        await PrintLine(
          $"Key-values:".Indent()
        );
        foreach (string key in httpReq.Session?.Keys)
        {
          await PrintLine(
            $"{key}: {httpReq.Session?.GetString(key)}"
            .Indent(2)
          );
        } */

      await PrintLine("RequestServices: (Provides services for request)");


      await PrintLine("Request: (HttpRequest)");
        await PrintLine("Body: (Stream)".Indent());
        using(
          StreamReader sr = new StreamReader(
            stream: httpReqResp.Request.Body,
            encoding: null,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: -1,
            leaveOpen: true
          )
        )
        {
          await PrintLine((await sr.ReadToEndAsync()).Indent(2));
          // string reqBody = sr.ReadToEnd();
        }
        await PrintLine(
          $"ContentLength: {httpReqResp.Request.ContentLength}"
          .Indent()
        );
        await PrintLine(
          $"ContentType: {httpReqResp.Request.ContentType}"
          .Indent()
        );
        await PrintLine("Cookies: (IRequestCookieCollection)".Indent());
        foreach(var cookie in httpReqResp.Request.Cookies)
        {
          await PrintLine($"{cookie.Key}: {cookie.Value}".Indent(2));
        }
        // content type must be form to read this...
        await PrintLine($"HasFormContentType: {httpReqResp.Request.HasFormContentType}".Indent());
        if(httpReqResp.Request.HasFormContentType)
        {
        await PrintLine("Form: (IFormCollection)".Indent());
        foreach(var form in httpReqResp.Request.Form)
        {
          await PrintLine($"{form.Key}: [{string.Join(", ", form.Value)}]".Indent(2));
        }
        }
        await PrintLine("GetTypedHeaders(): (RequestHeaders)".Indent());
          var typedHeaders = httpReqResp.Request.GetTypedHeaders();
          
          await PrintLine("Accept: ([MediaTypeHeaderValue])".Indent(2));
          foreach(var header in typedHeaders.Accept)
          {
            await PrintLine("MediaTypeHeaderValue".Indent(3));
              await PrintLine($"Boundary: {header.Boundary}".Indent(4));
              await PrintLine($"Charset: {header.Charset}".Indent(4));
              await PrintLine($"Encoding: {header.Encoding}".Indent(4));
              await PrintLine($"Encoding.WebName: {header.Encoding?.WebName}".Indent(4));
              await PrintLine(
                $"Facets: {string.Join(", ", header.Facets)}".Indent(4)
              );
              await PrintLine($"MediaType: {header.MediaType}".Indent(4));
              await PrintLine($"Parameters:".Indent(4));
              foreach (var parameter in header.Parameters)
              {
                await PrintLine($"{parameter.Name}: {parameter.GetUnescapedValue()}".Indent(5));
              }
              await PrintLine($"Quality: {header.Quality}".Indent(4));
              await PrintLine($"SubType: {header.SubType}".Indent(4));
              await PrintLine($"Type: {header.Type}".Indent(4));
          }
          await PrintLine("AcceptCharset: ([StringWithQualityHeaderValue])".Indent(2));
          foreach(var acceptCharset in typedHeaders.AcceptCharset)
          {
            await PrintLine($"{acceptCharset.Value}: {acceptCharset.Quality}".Indent(3));
          } 
          await PrintLine("AcceptEncoding: ([StringWithQualityHeaderValue])".Indent(2));
          foreach(var acceptEnc in typedHeaders.AcceptEncoding)
          {
            await PrintLine($"{acceptEnc.Value}: {acceptEnc.Quality}".Indent(3));
          } 
          await PrintLine("AcceptLanguage: ([StringWithQualityHeaderValue])".Indent(2));
          foreach(var acceptLang in typedHeaders.AcceptLanguage)
          {
            await PrintLine($"{acceptLang.Value}: {acceptLang.Quality}".Indent(3));
          } 
          await PrintLine($"CacheControl: (CacheControlHeaderValue".Indent(2));
            await PrintLine(typedHeaders.CacheControl?.ToString().Indent(3));
          await PrintLine("CacheControl: (CacheControlHeaderValue)".Indent(2));
            await PrintLine(
              $"MaxAge: {typedHeaders.CacheControl?.MaxAge}".Indent(3)
            );
            await PrintLine(
              $"MaxAge: {typedHeaders.CacheControl?.MaxStale}".Indent(3)
            );
            await PrintLine(
              $"Other parameters...".Indent(3)
            );
          await PrintLine("ContentDisposition: (ContentDispositionHeaderValue)".Indent(2));
            await PrintLine(
              typedHeaders.ContentDisposition?.ToString().Indent(3)
            );
          await PrintLine("ContentDisposition: (ContentDispositionHeaderValue)".Indent(2));
            await PrintLine(
              $"CreationDate: {typedHeaders.ContentDisposition?.CreationDate}".Indent(3)
            );
            await PrintLine(
              $"DispositionType: {typedHeaders.ContentDisposition?.DispositionType}".Indent(3)
            );
            await PrintLine(
              $"FileName: {typedHeaders.ContentDisposition?.FileName}".Indent(3)
            );
            await PrintLine(
              $"FileNameStar: {typedHeaders.ContentDisposition?.FileNameStar}".Indent(3)
            );
            await PrintLine(
              $"ModificationDate: {typedHeaders.ContentDisposition?.ModificationDate}".Indent(3)
            );
            await PrintLine(
              $"Name: {typedHeaders.ContentDisposition?.Name}".Indent(3)
            );
            await PrintLine(
              $"Parameters: ([NameValueHeaderValue])".Indent(3)
            );
            
            foreach (
              var item in typedHeaders.ContentDisposition?.Parameters 
                ?? Enumerable.Empty<NameValueHeaderValue>()
            )
            {
              await PrintLine(
                $"{item.Name}: {item.Value}".Indent(4)
              );
            }
            await PrintLine(
              $"ReadDate: {typedHeaders.ContentDisposition?.ReadDate}".Indent(3)
            );
            await PrintLine(
              $"Size: {typedHeaders.ContentDisposition?.Size}".Indent(3)
            );
          await PrintLine($"ContentLength: {typedHeaders.ContentLength}".Indent(2));
          await PrintLine($"ContentRange: (ContentRangeHeaderValue)".Indent(2));
            await PrintLine($"From: {typedHeaders.ContentRange?.From}".Indent(3));
            await PrintLine($"To: {typedHeaders.ContentRange?.To}".Indent(3));
            await PrintLine($"Unit: {typedHeaders.ContentRange?.Unit}".Indent(3));
          await PrintLine($"ContentType: (MediaTypeHeaderValue)".Indent(2));
            await PrintLine(typedHeaders.ContentType?.ToString().Indent(3));
          await PrintLine($"Cookie: (CookieHeaderValue)".Indent(2));
          foreach (var item in typedHeaders.Cookie)
          {
            await PrintLine($"{item.Name}: {item.Value}".Indent(3));
          }
          await PrintLine($"Date: {typedHeaders.Date}".Indent(2));
          await PrintLine($"Expires: {typedHeaders.Expires}".Indent(2));
          await PrintLine($"Host: {typedHeaders.Host}".Indent(2));
          await PrintLine($"IfMatch: ([EntityTagHeaderValue])".Indent(2));
          foreach (var item in typedHeaders.IfMatch)
          {
            await PrintLine($"{item.Tag}, is weak: {item.IsWeak}".Indent(3));
          }
          await PrintLine($"IfModifiedSince: {typedHeaders.IfModifiedSince}".Indent(2));
          await PrintLine($"IfNoneMatch: ([EntityTagHeaderValue])".Indent(2));
          foreach (var item in typedHeaders.IfMatch)
          {
            await PrintLine($"{item.Tag}, is weak: {item.IsWeak}".Indent(3));
          }
          await PrintLine($"IfRange: {typedHeaders.IfRange}".Indent(2));
          await PrintLine($"IfUnmodifiedSince: {typedHeaders.IfUnmodifiedSince}".Indent(2));
          await PrintLine($"LastModified: {typedHeaders.LastModified}".Indent(2));
          await PrintLine($"Range: (RangeHeaderValue)".Indent(2));
            await PrintLine($"Unit: {typedHeaders.Range?.Unit}".Indent(3));
            await PrintLine($"Ranges: ([RangeItemHeaderValue])".Indent(3));
            foreach (
              var item in typedHeaders.Range?.Ranges 
                ?? Enumerable.Empty<RangeItemHeaderValue>()
            )
            {
              await PrintLine($"From: {item.From}, to: {item.To}".Indent(4));
            }
          await PrintLine($"Referer: {typedHeaders.Referer}".Indent(2));
          
        await PrintLine("Headers: (IHeaderDictionary)".Indent());
        foreach(var header in httpReqResp.Request.Headers)
        {
          await PrintLine($"{header.Key}: {header.Value}".Indent(2));
        }
        
        await PrintLine($"Host: {httpReqResp.Request.Host}".Indent());
        await PrintLine($"IsHttps: {httpReqResp.Request.IsHttps}".Indent());
        await PrintLine($"Method: {httpReqResp.Request.Method}".Indent());
        await PrintLine($"Path: {httpReqResp.Request.Path.Value}".Indent());
        await PrintLine($"PathBase: {httpReqResp.Request.PathBase.Value}".Indent());
        await PrintLine($"Protocol: {httpReqResp.Request.Protocol}".Indent());
        await PrintLine($"Query: (IQueryCollection)".Indent());
        foreach (var item in httpReqResp.Request.Query)
        {
          await PrintLine($"{item.Key}: {item.Value}".Indent(2));
        }
        await PrintLine($"QueryString: {httpReqResp.Request.QueryString.Value}".Indent());
        await PrintLine($"Scheme: {httpReqResp.Request.Scheme}".Indent());
        await PrintLine($"RouteValues: (RouteValueDictionary)".Indent());
        foreach (var item in httpReqResp.Request.RouteValues)
        {
          await PrintLine($"{item.Key}: {item.Value}".Indent(2));
        }

      await PrintLine($"Response: (HttpResponse)".Indent(0));
        await PrintLine($"Headers: (HttpResponse)".Indent(1));
        foreach (var item in httpReqResp.Response.Headers)
        {
          await PrintLine($"{item.Key}: {item.Value}".Indent(2));
        }
        await PrintLine($"GetTypedHeaders(): (ResponseHeaders)".Indent(1));
          var responseHeaders = httpReqResp.Response.GetTypedHeaders();
          await PrintLine($"Location: {responseHeaders.Location}".Indent(2));
          await PrintLine($"ETag: {responseHeaders.ETag}".Indent(2));
          await PrintLine($"SetCookie: [SetCookieHeaderValue]".Indent(2));
          foreach (var item in responseHeaders.SetCookie)
          {
            await PrintLine($"Domain: {item.Domain}".Indent(3));
            await PrintLine($"Expires: {item.Expires}".Indent(3));
            await PrintLine($"HttpOnly: {item.HttpOnly}".Indent(3));
            await PrintLine($"MaxAge: {item.MaxAge}".Indent(3));
            await PrintLine($"Name: {item.Name}".Indent(3));
            await PrintLine($"Path: {item.Path}".Indent(3));
            await PrintLine($"SameSite: {item.SameSite}".Indent(3));
            await PrintLine($"Secure: {item.Secure}".Indent(3));
            await PrintLine($"Value: {item.Value}".Indent(3));
          }
        await PrintLine($"ContentType: {httpReqResp.Response.ContentType}".Indent(1));
        await PrintLine($"ContentLength: {httpReqResp.Response.ContentLength}".Indent(1));
        await PrintLine($"HasStarted: {httpReqResp.Response.HasStarted}".Indent(1));
        await PrintLine($"StatusCode: {httpReqResp.Response.StatusCode}".Indent(1));
        await PrintLine($"SupportsTrailers(): {httpReqResp.Response.SupportsTrailers()}".Indent(1));

      // https://stackoverflow.com/questions/15730308/how-to-find-if-a-member-variable-is-readonly
      // https://docs.microsoft.com/en-us/dotnet/api/system.type.getfields
      // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.fieldinfo.getvalue
      // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.fieldinfo.isinitonly
      // https://stackoverflow.com/questions/12480279/iterate-through-properties-of-static-class-to-populate-list
      await PrintLine($"HeaderNames:".Indent(0));
      Type type = typeof(HeaderNames);
      foreach (var item in type.GetFields(BindingFlags.Public | BindingFlags.Static))
      {
        await PrintLine($"{item.Name}: {item.GetValue(null)}".Indent(1));
      }
      
    }
  }

}