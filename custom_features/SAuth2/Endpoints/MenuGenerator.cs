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

using Microsoft.Extensions.DependencyInjection; // IServiceCollection
using Microsoft.AspNetCore.Builder; // IApplicationBuilder
using SAuth2.Extensions;            // Indent

namespace SAuth2.Endpoints
{
  public class MenuGenerator
  {
    private LinkGenerator linkGenerator;
    private EndpointDataSource endpointDataSource;

    public MenuGenerator(
      LinkGenerator linkGenerator,
      EndpointDataSource endpointDataSource
    )
    {
      this.linkGenerator = linkGenerator;
      this.endpointDataSource = endpointDataSource;
    }



    public async Task Endpoint(HttpContext httpContext)
    {
      // https://benfoster.io/blog/aspnetcore-3-1-current-route-endpoint-name/
      var endpointsNames = endpointDataSource.Endpoints
        .Select(e => e.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName ?? "");
      var endpointsPaths = endpointsNames
          .Select(name => 
            $"{name}: " + linkGenerator.GetUriByName(
              httpContext: httpContext, 
              endpointName: name, 
              values: null
            )
          );
      
      string text = 
        "Menu:\n"
        + string.Join("\n", endpointsPaths).Indent(1);
      
      httpContext.Response.ContentType = "text/plain";
      await httpContext.Response.WriteAsync(text.Lf());
    }




    public RequestDelegate TerminalMiddleware( 
      RequestDelegate next
    ) 
    {
      async Task EndpointAndMiddleware(HttpContext httpContext)
      {
        await Endpoint(httpContext);
        // since response is generated, it should be terminall middelware
        // await next(httpContext);
      };

      return EndpointAndMiddleware;
    }


    
  }

  public static class MenuGeneratorExt
  {
    public static IServiceCollection AddMenuGenerator(
      this IServiceCollection services
    ) => services.AddTransient<MenuGenerator>();

    public static Func<RequestDelegate,RequestDelegate> GetMenuGeneratorTerminal(
      this IApplicationBuilder app
    )
    {
      MenuGenerator menuGenerator = app
        .ApplicationServices.GetService<MenuGenerator>();
      
      return menuGenerator.TerminalMiddleware;
    }

    public static RequestDelegate GetMenuGeneratorEndpoint(this IApplicationBuilder app)
    {
      MenuGenerator menuGenerator = app
        .ApplicationServices.GetService<MenuGenerator>();
      
      return menuGenerator.Endpoint;
    }
  }




}