using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;       // Task
using Microsoft.AspNetCore.Http;    // HttpContext
using Microsoft.AspNetCore.Routing; // IEndpointRouteBuilder
                                    // HttpContex.GetRouteData
using Microsoft.Extensions.DependencyInjection;
using SAuth2.Extensions;            // Indent

namespace SAuth2.Endpoints
{
  
  public static class InspectEndpointsFunctions
  {
    
    public static async Task InspectEndpointsReqFun(HttpContext httpReqResp)
    {
      var endpointDs = httpReqResp          // Services and their dependencies 
        .RequestServices                    // are resolved from the 
          .GetService<EndpointDataSource>();// RequestServices collection.
                                            // The framework creates a scope per 
                                            // request and RequestServices 
                                            // exposes the scoped service 
                                            // provider. All scoped services are 
                                            // valid for as long as the request 
                                            // is active.
      
      var services = httpReqResp.RequestServices;
      // var xx = services.GetServices<EndpointDataSource>();

      var title  = "Hellow from InspectEndpointsReqFun(httpReqResp)";
      var head   = "App all endpoints informations.";
      await httpReqResp.Response.WriteAsync(title.Lf() + head.Lf());


      var infos = endpointDs.Endpoints.StringifyPretty(
        stringify: e => "{"
          + $"\n\tDisplayName: {e.DisplayName}"
          + $"\n\tMetadata: \n{e.Metadata.StringifyPretty().Indent(1)}"
          + $"\n\tToString(): {e}"
          + "\n}"
      );

      await httpReqResp.Response.WriteAsync(infos.Lf());

      IEnumerable<RouteEndpoint> routeEndpoints = 
        endpointDs.Endpoints.OfType<RouteEndpoint>();
      
      var infoRoutePattern = routeEndpoints.FirstOrDefault()?.RoutePattern;
      // infoRoutePattern.ParameterPolicies[""].First().ParameterPolicy

    }

    
    

    
    
  }

}