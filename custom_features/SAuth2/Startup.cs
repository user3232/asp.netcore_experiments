#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing; // IEndpointRouteBuilder
                                    // HttpContext.GetRouteData()
using Microsoft.AspNetCore.Routing.Patterns; // RoutePattern
using Microsoft.Extensions.Configuration;

using Ends = SAuth2.Endpoints;
using MultiEnd = SAuth2.Endpoints.MiddlewareToEndpoint.MultiMidEndpoint;
using InspectEndsTermMid = SAuth2.Middleware.InspectEndpoints; 
using BranchingEgz = SAuth2.Endpoints.AsApplicationBuilders.BranchingExamples;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting.Server.Features;
using SAuth2.Extensions;

namespace SAuth2
{

  
  public class Startup
  {
    // This method gets called by the runtime. Use this method to
    // add services to the container. For more information on how
    // to configure your application, visit
    // https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services, IConfiguration conf)
    {
      Ends.MenuGeneratorExt.AddMenuGenerator(services);
      // configures session options
      services.Configure<SessionOptions>(opts => {});
      
      // services.AddOptions<object>().Bind<object>(conf);
    }

    // This method gets called by the runtime. Use this method to
    // configure the HTTP request pipeline.
    public void Configure(
      IApplicationBuilder app, 
      IWebHostEnvironment env,
      IConfiguration conf,
      IHostApplicationLifetime lifetime
    )
    {
      
      
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseRouting();
      
      // HttpContext httpContext = null!;
      // httpContext.Features
      
      // app.ServerFeatures
      // app.UseMiddleware<InspectEndsTermMid.AsClass>(); 
      // app.Use(MenuGenerator.GetTerminalMiddleware(app));

      app.UseEndpoints(
        configure: (IEndpointRouteBuilder endpoints) =>
        {
          
          endpoints
            .MapGet(                    
              pattern: "/",             
              requestDelegate: MultiEnd.FewTerminalsEndpoint
            )
            .WithDisplayName(displayName: "Info")
            .WithMetadata(new EndpointNameMetadata("Info"))
                                        // https://benfoster.io/blog/aspnetcore-3-1-current-route-endpoint-name/
            .WithMetadata("Yo metadata!");   


          endpoints
            .MapGet(
              pattern: "/endpoints",
              requestDelegate: Ends.InspectEndpointsFunctions.InspectEndpointsReqFun
            )
            .WithMetadata(new EndpointNameMetadata("EndpointsInfo"))
            .WithDisplayName("EndpointsInfo");


          endpoints.Map(
            pattern: RoutePatternFactory.Parse(
              pattern: "/some-endpoint"
            ),
            requestDelegate: x => Task.CompletedTask
          );


          MultiEnd.MapFewTerminalsEndpoint(
            endpoints: endpoints,
            pattern: "/MenuTermAndEndpointsTerm"
          );
          

          // https://andrewlock.net/how-to-automatically-choose-a-free-port-in-asp-net-core/
          endpoints.MapGet(
            "/addresses",
            endpoints
              .CreateApplicationBuilder()
                .Use(
                  async (httpContext, nextRequestDelegate) => 
                  {
                    var runtimeAddresses = httpContext.Features
                      .Get<IServerAddressesFeature>()
                        ?.Addresses
                          ?.AsEnumerable()
                        ?? Enumerable.Empty<string>();
                    
                    await httpContext.Response.WriteAsync(
                      "Server runtime addresses: "
                      + runtimeAddresses.StringifyPretty().Lf()
                    );
                  }
                )
                  .Build()
          );


        }
      );
    }

    
  }

  

  

  

  

  




}

#nullable restore
