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

  /* 
      Usage for middleware:

    services.AddTransient<SAuth2.Middleware.InspectEndpoints.AsLambdaWithDi>();
                                        // Object containing middleware
                                        // functions which additional
                                        // parameters are captured by DI.
                                        // (Cannot be AddSingleton because
                                        // parameters would be captured
                                        // only once.)

    app.Use(
        app.ApplicationServices             // endpoints inspection midleware
          .GetService<AsLambdaWithDi>()     // Same as above but first
            .Run1                           // added to container, here taken
                                            // and used its function:
                                            // Task Run1(HttpContext, Func<Task>)
    );

    app.Use(
      app.ApplicationServices             // endpoints inspection midleware
        .GetService<AsLambdaWithDi>()     // Same as above but function with
          .Run2                           // different signature is used:
                                          // RequestDelegate Run2(RequestDelegate)
    );


   */


  public class AsLambdaWithDi
  {
    EndpointDataSource endpointDs;
    LinkGenerator linkGenerator;

    public AsLambdaWithDi(
      EndpointDataSource endpointDs,  //   arbitrary to be injected
      LinkGenerator linkGenerator     //   arbitraryto be injected
    )
    {
      this.endpointDs = endpointDs;
      this.linkGenerator = linkGenerator;
    }

    #region Middleware as functions

    public async Task Run1(         // method to be consumen by
                                   //   app.Use((context, next) => ....)
      HttpContext httpContext,     // 
      Func<Task> nextMiddleware    // 
    )
    {
      var title  = "Hellow from InspectEndpointsDi.Run(HttpContext, Func<Task>)";
      var head = "App all endpoints informations.";
      await httpContext.Response.WriteAsync(title.Lf() + head.Lf());

      var infos = endpointDs.Endpoints.StringifyPretty(
        stringify: e => "{"
          + $"\n\tDisplayName: {e.DisplayName}"
          + $"\n\tMetadata: \n{e.Metadata.StringifyPretty().Indent(1)}"
          + "\n}"
      );

      await httpContext.Response.WriteAsync(infos.Lf());
      // since response is generated, it should be terminall middelware
      // await nextMiddleware();
    }

    public RequestDelegate Run2(       // method to be consumen by
                                      //   app.Use(next => context => ...)
      RequestDelegate nextMiddleware  // 
    )
    {
      async Task RunInternal(HttpContext httpContext) 
      {
        var title  = "Hellow from InspectEndpointsDi.Run(next => context => ...)";
        var head  = "App all endpoints informations.";
        await httpContext.Response.WriteAsync(title.Lf() + head.Lf());


        var infos = endpointDs.Endpoints.StringifyPretty(
          stringify: e => "{"
            + $"\n\tDisplayName: {e.DisplayName}"
            + $"\n\tMetadata: \n{e.Metadata.StringifyPretty().Indent(1)}"
            + "\n}"
        );

        await httpContext.Response.WriteAsync(infos.Lf());
        // since response is generated, it should be terminall middelware
        // await nextMiddleware(httpContext);
      }
      return RunInternal;
    }

    #endregion
  }

}