using B = Microsoft.AspNetCore.Builder;
using Brun = Microsoft.AspNetCore.Builder.RunExtensions;
using Bmap = Microsoft.AspNetCore.Builder.MapExtensions;
using BmapWhen = Microsoft.AspNetCore.Builder.MapWhenExtensions;
using Rwrite = Microsoft.AspNetCore.Http.HttpResponseWritingExtensions;

namespace SAuth2.Endpoints.AsApplicationBuilders
{

  public static class BranchingExamples
  {
    public static void HandleMapTest1(B.IApplicationBuilder app)
    {
      Brun.Run(
        app: app,
        handler: async context =>
        {
          await Rwrite.WriteAsync(
            response: context.Response,
            text: "Map Test 1"
          );
        }
      );
    }

    public static void HandleMapTest2(B.IApplicationBuilder app)
    {
      Brun.Run(
        app: app,
        handler: async context =>
        {
          await Rwrite.WriteAsync(
            response: context.Response,
            text: "Map Test 2"
          );
        }
      );
    }

    public static void Configure(B.IApplicationBuilder app)
    {
      Bmap.Map(app: app, pathMatch: "/map1", configuration: HandleMapTest1);
      Bmap.Map(app: app, pathMatch: "/map2", configuration: HandleMapTest2);

      
      Brun.Run(
        app: app,
        handler: async context =>
        {
          await Rwrite.WriteAsync(
            response: context.Response,
            text: "Hello from non-Map delegate. <p>"
          );
        }
      );
    }
  }
}