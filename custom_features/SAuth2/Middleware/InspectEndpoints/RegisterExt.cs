using System;
using System.Threading.Tasks;       // Task
using Microsoft.AspNetCore.Http;    // HttpContext
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder; // IApplicationBuilder


namespace SAuth2.Middleware.InspectEndpoints
{

  /* 
      Usage:

    app.ExperimentWithMidDifferentFormats();
  
   */

  
  public static class RegisterExt 
  {
    public static IApplicationBuilder UseInspectEndpoints(
      this IApplicationBuilder builder
    )
    {
      return builder.UseMiddleware<AsClass>();
    }


    public static IApplicationBuilder ExperimentWithMidDifferentFormats(
      this IApplicationBuilder app
    )
    {
      app.Use(                                // custom inline middleware
        (RequestDelegate reqFunction) => 
          (HttpContext httpReqResp) => 
          {
            return reqFunction(httpReqResp);
            // end pipeline:
            // return Task.CompletedTask;
          }
      );

      app.Use(                                // custom inline middleware
        /* middleware_fun */ (
          HttpContext httpContext,
          Func<Task>  next
        ) =>
        {
          return next();
          // end pipeline:
          // return Task.CompletedTask;
        }
      );


      app.Use(
        AsLambda          // endpoints inspection midleware
          .InspectEndpointsMidFun1
      ); 
      app.Use(
        AsLambda          // endpoints inspection midleware
          .InspectEndpointsMidFun2    // Same as above with different 
                                      // signature and args passing
      ); 
      app.UseMiddleware<AsClass>(); 
                                      // endpoints inspection midleware
                                      // Same as above but used as class
      app.UseInspectEndpoints();
                                      // endpoints inspection midleware
                                      // Same as above but registered by
                                      // extension
      app.Use(
        app.ApplicationServices             // endpoints inspection midleware
          .GetService<AsLambdaWithDi>() // Same as above but first
            .Run1                           // added to container, here taken
                                            // and used its function:
                                            // Task Run1(HttpContext, Func<Task>)
      );
      app.Use(
        app.ApplicationServices             // endpoints inspection midleware
          .GetService<AsLambdaWithDi>() // Same as above but function with
            .Run2                           // different signature is used:
                                            // RequestDelegate Run2(RequestDelegate)
      );
      return app;
    }
  }

}