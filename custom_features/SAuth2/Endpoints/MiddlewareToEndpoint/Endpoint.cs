using System.Threading.Tasks;       // Task
using Microsoft.AspNetCore.Http;    // HttpContext
using Microsoft.AspNetCore.Routing; // IEndpointRouteBuilder
                                    // HttpContex.GetRouteData
using Microsoft.Extensions.DependencyInjection;
using SAuth2.Extensions;            // Indent
using Microsoft.AspNetCore.Builder; // IEndpointConventionBuilder
using Ends = SAuth2.Endpoints;


namespace SAuth2.Endpoints.MiddlewareToEndpoint
{
  public static class MultiMidEndpoint
  {

    public static async Task FewTerminalsEndpoint(HttpContext httpContext)
    {
      // Comment:
      //
      // Bouth functions writes response, but since they are executed
      // synchronousely it is no problem!!!
      // =>
      //    Middleware is executed asynchronously and its needed
      //    computations are synchronised at endpoint or terminal as
      //    it is here. => response stream must be fully constructed
      //    before sending http response. 
      // =>
      //    header content-length must be computed before sending
      //    http. (content to send buffer must be fully populated or
      //    content length must be known beforehand, as in case of
      //    sending file, of course file may be considered as fully
      //    populated buffer, but there may be other examples).

      // await Ends.MenuGeneratorExt.GetMenuGeneratorEndpoint(app)(httpContext);
      var menuGen = httpContext.RequestServices.GetService<MenuGenerator>();
      await menuGen.Endpoint(httpContext);
      await Ends.ReqResInfo.Print(httpContext);
    }

    
    /// <summary>
    /// This shows how to use few terminal middlewares at once
    /// (without causing error) if terminals are converted
    /// to endpoint.
    /// </summary>
    /// <param name="endpoints">Endpoints structure to **mutate**!!!</param>
    /// <param name="pattern">Configured endpoint url (pattern).</param>
    /// <returns>Builded endpoint structure (allowing modifications/mutations).</returns>
    public static IEndpointConventionBuilder MapFewTerminalsEndpoint(
      this IEndpointRouteBuilder endpoints, 
      string pattern
    )
    {
      RequestDelegate pipeline = endpoints
        .CreateApplicationBuilder()             // factory for (sub) app
          .UseMiddleware<TerminalMiddleware1>() // adds middleware to builder
          .UseMiddleware<TerminalMiddleware2>() // adds middleware to builder
            .Build();                           // makes RequestDelegate

      return endpoints                          // endpoints structure mutation
        .Map(
          pattern:          pattern, 
          requestDelegate:  pipeline
        )
        .WithDisplayName("Composed terminal mids as endpoint");
    }
    
  }

}