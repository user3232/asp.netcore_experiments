using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DbUp;

using QandA.Data;
using QandA.Hubs;

namespace QandA
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      #region DbUp
      var connectionString = Configuration.GetConnectionString("DefaultConnection");
      // EnsureDatabase.For.SqlDatabase(connectionString);
      // // Create and configure an instance of the DbUp upgrader
      // var upgrader = DeployChanges.To.SqlDatabase(connectionString, null)
      //   .WithScriptsEmbeddedInAssembly(
      //     System.Reflection.Assembly.GetExecutingAssembly()
      //   )
      //   .WithTransaction()
      //   .Build();
      // // Do a database migration if there are any pending SQL
      // //Scripts
      // if (upgrader.IsUpgradeRequired())
      // {
      //   upgrader.PerformUpgrade();
      // }
      #endregion

      #region MVC
      services.AddControllers();
      # endregion

      #region Data Connection Drivers Facades
      /* 
        Adding dependencies to DI container:
        - AddScoped = create instance for every http request
        - (AddTransient will generate a new instance of the class each time it is requested.)
        - (AddSingleton will generate only one class instance for the lifetime of the whole app.)

        IDataRepository is key (name of dependency in DI)
        DataRepository is value (implementation of dependency)
      */

      services.AddSingleton<MemoryDb>();
      services.AddSingleton<IDataRepository, MemDataRepository>();

      // AddScoped is usefull because of multi-threading
      // request is served by one thread usually
      // services.AddScoped<IDataRepository, DataRepository>();
      # endregion


      #region Access Control
      // https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS
      // https://developer.mozilla.org/en-US/docs/Web/HTTP
      // https://www.html5rocks.com/en/tutorials/internals/howbrowserswork/
      services.AddCors(
        options =>
          options.AddPolicy(
            "CorsPolicy", 
            builder =>
              builder.AllowAnyMethod()
              .AllowAnyHeader()
              // in dev webapp is served from localhost:3000
              .WithOrigins("http://localhost:3000")
              .AllowCredentials()
          )
      );
      # endregion

      #region Data Hub
      services.AddSignalR();
      #endregion

      #region Cache
      services.AddMemoryCache();
      services.AddSingleton<IQuestionCache, QuestionCache>();
      #endregion
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseCors("CorsPolicy");

      
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        // use https only in production
        // this is because in developement
        // frontend will be served by http express server
        // and services will be served by this ASP Core server
        // and https should not mix with http
        app.UseHttpsRedirection();
      }


      app.UseRouting();

      app.UseAuthorization();

      // endpoints managed by MVC (Controllers)
      app.UseEndpoints(endpoints =>
      {
        // routes are implied by controller classes naming and attributes:
        endpoints.MapControllers();
        // route for datahub:
        endpoints.MapHub<QuestionsHub>("/questionshub");
      });
    }
  }
}
