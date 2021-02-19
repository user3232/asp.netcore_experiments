using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;       // Task
using Microsoft.AspNetCore.Http;    // HttpContext
using Microsoft.AspNetCore.Routing; // IEndpointRouteBuilder
                                    // HttpContex.GetRouteData
using Microsoft.Extensions.DependencyInjection;
using SAuth2.Extensions;            // Indent

namespace SAuth2.Middleware.InspectEndpoints
{

  public static class AsLambda
  {
    public static RequestDelegate InspectEndpointsMidFun1(
      RequestDelegate nextMiddleware
    )
    {
      Task InspectAndCallNextMiddleware(HttpContext httpReqResp)
      {
        var title  = "Hellow from InspectEndpointsMidFun1(nextMiddleware):";
        var head   = "App all endpoints informations.";
        httpReqResp.Response.WriteAsync(title.Lf() + head.Lf()).Wait();

        var endpointDs = httpReqResp
          .RequestServices    
            .GetService<EndpointDataSource>();

        var infos = endpointDs.Endpoints.StringifyPretty(
          stringify: e => "{"
            + $"\n\tDisplayName: {e.DisplayName}"
            + $"\n\tMetadata: \n{e.Metadata.StringifyPretty().Indent(1)}"
            + "\n}"
        );

        httpReqResp.Response.WriteAsync(infos.Lf()).Wait();
        // return nextMiddleware(httpReqResp);
        // since response is generated, it should be terminall middelware
        return Task.CompletedTask;
      }
      return InspectAndCallNextMiddleware;
    }


    public static RequestDelegate InspectEndpointsMidFun2(
      RequestDelegate nextMiddleware
    )
    {
      async Task InspectAndCallNextMiddleware(HttpContext httpReqResp)
      {
        var title  = "Hellow from InspectEndpointsMidFun2(nextMiddleware):";
        var head   = "App all endpoints informations.";
        await httpReqResp.Response.WriteAsync(title.Lf() + head.Lf());

        var endpointDs = httpReqResp
          .RequestServices    
            .GetService<EndpointDataSource>();

        var infos = endpointDs.Endpoints.StringifyPretty(
          stringify: e => "{"
            + $"\n\tDisplayName: {e.DisplayName}"
            + $"\n\tMetadata: \n{e.Metadata.StringifyPretty().Indent(1)}"
            + "\n}"
        );

        await httpReqResp.Response.WriteAsync(infos.Lf());
        // since response is generated, it should be terminall middelware
        // await nextMiddleware(httpReqResp);
      }
      return InspectAndCallNextMiddleware;
    }
  }

}