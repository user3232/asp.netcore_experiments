using System.Linq;
using System.Threading.Tasks;       // Task
using Microsoft.AspNetCore.Http;    // HttpContext
using Microsoft.AspNetCore.Routing; // IEndpointRouteBuilder
                                    // HttpContex.GetRouteData
using Microsoft.Extensions.DependencyInjection;
using SAuth2.Extensions;            // Indent


namespace SAuth2.Endpoints.MiddlewareToEndpoint
{
  public class TerminalMiddleware1
  {
    private          EndpointDataSource endpointDataSource;
    private          LinkGenerator      linkGenerator;
    private readonly RequestDelegate    next;

    public TerminalMiddleware1 (            // constructor:
      RequestDelegate next,                 //   must have RequestDelegate
      EndpointDataSource endpointDataSource,//   arbitrary to be injected
      LinkGenerator linkGenerator           //   arbitraryto be injected
    )
    {
      this.endpointDataSource = endpointDataSource;
      this.linkGenerator = linkGenerator;
      this.next = next;
    }

    public async Task InvokeAsync( // method must be named Invoke or InvokeAsync
      HttpContext httpContext,     // to be mid first arg must be HttpContext!!
      LinkParser linkParser        // arbitrary (may be scoped) to be injected
    )
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
          "\n-------------------------------------------------"
        + "Menu:\n"
        + string.Join("\n", endpointsPaths).Indent(1)
        + "\n-------------------------------------------------";
        
      httpContext.Response.ContentType = "text/plain";
      await httpContext.Response.WriteAsync(text.Lf());

      // since response is generated, it should be terminall middelware
      // await next(httpContext);
    }
  }

}