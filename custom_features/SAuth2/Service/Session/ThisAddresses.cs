#nullable enable

using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Session;
using System.Collections.Generic;
// using Microsoft.AspNetCore.Http;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using ResponseHeaders = Microsoft.AspNetCore.Http.Headers.ResponseHeaders;
using Debug = System.Diagnostics.Debug;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Net;
using System.Reflection;
using SAuth2.Extensions;

using WebEncoders = Microsoft.AspNetCore.WebUtilities;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Diagnostics.CodeAnalysis;

namespace SAuth2.Service.Session.Api5
{
  /* 
    https://stackoverflow.com/questions/1612177/are-http-cookies-port-specific

    Cookie cannot specify port, cookies are available to all ports
    Cookies are also available to all subdomains.


    On what domain am I executing?:
      * socket knows ip address and port
      * from reverse mapping dns may be obtained
      * also http request host header may be checked
        * with trusted TLS connection server cert must be trusted
          and contain dns equals dns specified by client
  */

  

  public class GenericHostAddresses
  {
    
    /// <summary>
    /// Returns current addresses on which Kestrel or http.sys listens.
    /// </summary>
    /// <param name="httpContext">Current http context.</param>
    /// <returns>List of listen addresses.</returns>
    public string[] GetRuntimeServerAddresses(HttpContext httpContext)
      =>  httpContext.Features
            .Get<IServerAddressesFeature>()?.Addresses?.ToArray() 
            ?? new string[]{};

    public string GetHostFromContext(HttpContext httpContext)
      => httpContext.Request.Host.Value;

    public string[] GetConfigurationUrls(IConfiguration configuration)
      => configuration["urls"].Split(';');

    public (IPAddress, int) GetRuntimeRequestIpAndPort(HttpContext httpContext)
      => (httpContext.Connection.LocalIpAddress, httpContext.Connection.LocalPort);
      
  }


  public class KestrelHostAddresses
  {

    public string[] GetKestrelConfigurationSectionUrls(
      IConfiguration configuration
    )
      => configuration
          .GetSection("Kestrel")
            .GetSection("Endpoints")
              .GetChildren()
                .Select(conf => conf["url"]).ToArray();

    public string[] GetKestrelConfigurationUrls(
      KestrelServerOptions kestrelServerOptions
    )
      => kestrelServerOptions
          .Configure()
            .Configuration
              .GetSection("Endpoints")
                .GetChildren()
                  .Select(conf => conf["Url"])
                    .ToArray();

    public string[] GetWebHostBuilderKestrelServerAddresses(
      IWebHostBuilder hostWebHostBuilder
    )
    {
      IConfiguration kestrelConfiguration = null!;
      WebHostBuilderKestrelExtensions.ConfigureKestrel(
        hostBuilder: hostWebHostBuilder,
        configureOptions: (
          WebHostBuilderContext  context, 
          KestrelServerOptions   serverOptions
        ) =>
        {
          // add nothing new, but obtain cumulative configuration
          kestrelConfiguration = serverOptions.Configure().Configuration;
        }
      );
      return kestrelConfiguration
        .GetSection("Endpoints")
          .GetChildren()
            .Select(conf => conf["url"]).ToArray();
    }

    public IEnumerable<ListenOptions> HackKestrelRegisteredAddresses(
      KestrelServerOptions kestrelServerOptions
    )
    {
      var (succ, data) = kestrelServerOptions
        .GetInstanceProperty<IEnumerable<ListenOptions>>("ListenOptions");

      return succ ? data : Enumerable.Empty<ListenOptions>();
    }

    public string[] GetHackKestrelRegisteredAddresses(
      KestrelServerOptions kestrelServerOptions // can be obtained from: 
                                                // -> IServer
                                                //    -> KestrelServerImplementation
                                                //       -> Options
                                                // or by DI:
                                                //   provider.Get<KestrelServerOptions>()
    )
      => HackKestrelRegisteredAddresses(kestrelServerOptions)
          .Select(listenOptions => listenOptions.ToString()).ToArray();
    
  }



  public class UrlsOptions
  {
    public string urls {get;set;}
    public UrlsOptions() {urls = "";}
    public string[] GetUrls() => urls.Split(';').ToArray();


    public static UrlsOptions GetUrlOptionsByBind(IConfiguration configuration)
    {
      var urlOpts = new UrlsOptions();
      configuration.Bind(urlOpts);
      return urlOpts;
    }

    public static UrlsOptions GetUrlOptionsByBindFact(IConfiguration configuration)
      => configuration.Get<UrlsOptions>();
  }

  public class UrlsOptionsProvider
  {

    public UrlsOptionsProvider(
      IServiceCollection services,
      IConfiguration     configuration
    ) => AddUrlsOptions(services, configuration);


    /// <summary>
    /// Registers UrlsOptions.
    /// </summary>
    /// <param name="services">Services to add to.</param>
    /// <param name="configuration">
    /// Reference to configuration to be used when binding.
    /// </param>
    /// <returns>Mutated services collection.</returns>
    public IServiceCollection AddUrlsOptions(
      IServiceCollection services,
      IConfiguration     configuration
    )
      => services.Configure<UrlsOptions>(configuration);

    /// <summary>
    /// Provides UrlsOptions if serviceProvider is compatible
    /// with services from constructor.
    /// </summary>
    /// <param name="serviceProvider">
    /// DI container to look for UrlsOptions factory or
    /// living instance.
    /// </param>
    /// <returns>UrlsOptions or null</returns>
    public IOptionsMonitor<UrlsOptions>? GetUrlsOptionsByDi(
      IServiceProvider serviceProvider
    )
      => serviceProvider.GetService<IOptionsMonitor<UrlsOptions>>();
  }

}

#nullable restore