using System.Threading.Tasks;       // Task
using Microsoft.AspNetCore.Http;    // HttpContext
using Microsoft.AspNetCore.Routing; // IEndpointRouteBuilder
                                    // HttpContex.GetRouteData
using Microsoft.Extensions.DependencyInjection;
using SAuth2.Extensions;            // Indent


namespace SAuth2.Endpoints.MiddlewareToEndpoint
{
  public class TerminalMiddleware2
  {
    private          EndpointDataSource endpointDs;
    private          LinkGenerator      linkGenerator;
    private readonly RequestDelegate    next;

    public TerminalMiddleware2 (                  // constructor:
      RequestDelegate next,           //   must have RequestDelegate
      EndpointDataSource endpointDs,  //   arbitrary to be injected
      LinkGenerator linkGenerator     //   arbitraryto be injected
    )
    {
      this.endpointDs = endpointDs;
      this.linkGenerator = linkGenerator;
      this.next = next;
    }

    public async Task InvokeAsync( // method must be named Invoke or InvokeAsync
      HttpContext httpContext,     // to be mid first arg must be HttpContext!!
      LinkParser linkParser        // arbitrary (may be scoped) to be injected
    )
    {
      var title  = "Hellow from InspectEndpointsMid.InvokeAsync(HttpContext, LinkParser)";
      var head = "App all endpoints informations.";
      await httpContext.Response.WriteAsync(title.Lf() + head.Lf());

      var infos = endpointDs.Endpoints.StringifyPretty(
        stringify: e => "{"
          + $"\n\tDisplayName: {e.DisplayName}"
          + $"\n\tMetadata: \n{e.Metadata.StringifyPretty().Indent(1)}"
          + "\n}"
      );

      var text = 
            "\n-------------------------------------------------"
          + infos
          + "\n-------------------------------------------------";

      await httpContext.Response.WriteAsync(text.Lf());
      // since response is generated, it should be terminall middelware
      // await next(httpContext);
    }
  }
  

}