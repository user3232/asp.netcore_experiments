using System.Linq;
using System.Threading.Tasks;
using System.Threading; // CancellationToken

using Sys        = System;
using Glob       = System.Globalization; // NumberStyles
using Arr        = System.Collections.Generic;
using Io         = System.IO; // Directory.GetCurrentDirectory()
using HostBuild  = Microsoft.AspNetCore.Hosting.Builder; // ApplicationBuilderFactory
using Host       = Microsoft.AspNetCore.Hosting;
using HostExt    = Microsoft.Extensions.Hosting;
using HostExtInt = Microsoft.Extensions.Hosting.Internal;
using Log        = Microsoft.Extensions.Logging;
using ConfExtMem = Microsoft.Extensions.Configuration.Memory; // MemoryConfigurationSource
using Conf       = Microsoft.Extensions.Configuration;
using ConfEnv    = Microsoft.Extensions.Configuration.EnvironmentVariables;
using ConfComm   = Microsoft.Extensions.Configuration.CommandLine;
using ConfJson   = Microsoft.Extensions.Configuration.Json; // JsonConfigurationSource
using Di         = Microsoft.Extensions.DependencyInjection;
using DiExt      = Microsoft.Extensions.DependencyInjection.Extensions; // TryAddSingleton
using Opt        = Microsoft.Extensions.Options; // IOptions
using Http       = Microsoft.AspNetCore.Http; // DefaultHttpContextFactory, MiddlewareFactory

using NumStyles = System.Globalization.NumberStyles;
using ServCallDescExts = Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions; // TryAddSingleton




namespace SAuth2.HostExperiments
{
  public static class MyHostFactory
  {
    public static HostExt.IHost UpdateToWebHostStructure(MyHost host)
    {
      
      // https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/GenericHost/GenericWebHostBuilder.cs
      var configBuilder = new Conf.ConfigurationBuilder();
      Conf.MemoryConfigurationBuilderExtensions.AddInMemoryCollection(
        configBuilder
      );
      Conf.EnvironmentVariablesExtensions.AddEnvironmentVariables(
        configBuilder, prefix: "ASPNETCORE_"
      );

      Di.IServiceCollection services = null;
      DiExt.ServiceCollectionDescriptorExtensions
        .TryAddSingleton<Http.IHttpContextFactory, Http.DefaultHttpContextFactory>(services);
      DiExt.ServiceCollectionDescriptorExtensions
        .TryAddScoped<Http.IMiddlewareFactory, Http.MiddlewareFactory>(services);
      DiExt.ServiceCollectionDescriptorExtensions
        .TryAddSingleton<HostBuild.IApplicationBuilderFactory, HostBuild.ApplicationBuilderFactory>(services);


      return null;
    }



    public static MyHost CreateHostStructure(string[] args)
                                        // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Hosting/src/Host.cs
    {
      // HostBuilder hostBuilder = new HostBuilder();
                                        // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Hosting/src/HostBuilder.cs

      var hostConfigBuilder = new Conf.ConfigurationBuilder();
                                    // Sets up root path:
                                    // IHostBuilder.UseContentRoot(...)
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs
      Conf.IConfigurationSource memConfigSrc =      // Additional config source can always be added.
                                                // Source is only for getting provider 
        new ConfExtMem.MemoryConfigurationSource() // provider have interface aka JSON: 
        {                                      // IConfigurationProvider mcs = memConfigSrc.Build() 
          InitialData = new[]                  // mcs.TryGet(key, out val) -> string 
          {                                    // mcs.Set(key, value)
            new Arr.KeyValuePair<string, string>( // mcs.Load() // loads 
              HostExt.HostDefaults.ContentRootKey,     // msc.GetChildKeys( 
              Io.Directory.GetCurrentDirectory()  //   addThoseKeysToResult,
            )                                  //   parentPath
          }                                    // ) -> [key]
        };                                       
                                    
      hostConfigBuilder.Add(source: memConfigSrc);
                                    // hostConfigBuilder is list of config sources
                                    // which gives list of config providers
                                    // wihich gives list of readers/writers with
                                    // JSON like interface.
                                    // configProviders may be many, and last with
                                    // existing key is used to return key-value
      hostConfigBuilder.Add(
        source: new ConfEnv.EnvironmentVariablesConfigurationSource 
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.EnvironmentVariables/src/EnvironmentVariablesConfigurationProvider.cs
        {                           // Reads environment variables:
          Prefix = "DOTNET_"        // starting with DOTNET_
          // MySqlServerPrefix = "MYSQLCONNSTR_"
                                    // -> [$"ConnectionStrings:{key}"] = value
                                    // -> [$"ConnectionStrings:{key}_ProviderName"] 
                                    //    = "MySql.Data.MySqlClient"
          // SqlAzureServerPrefix = "SQLAZURECONNSTR_"
                                    // -> [$"ConnectionStrings:{key}"] = value
                                    // -> [$"ConnectionStrings:{key}_ProviderName"]
                                    //    = "System.Data.SqlClient"
          // SqlServerPrefix = "SQLCONNSTR_"
                                    // -> [$"ConnectionStrings:{key}"] = value
                                    // -> [$"ConnectionStrings:{key}_ProviderName"]
                                    //    = "System.Data.SqlClient"
          // CustomPrefix = "CUSTOMCONNSTR_"
                                    // -> [$"ConnectionStrings:{key}"] = value
        }
      );

      hostConfigBuilder.Add(
        new ConfComm.CommandLineConfigurationSource 
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.CommandLine/src/CommandLineConfigurationProvider.cs
        {
          Args = new[] {"-short", "shortVal", "--long", "15"},
                                    // -short shortVal --long 15
          SwitchMappings = new Arr.Dictionary<string,string>
          {
            ["-short"] = "key1",
            ["--long"] = "key2",
          }
        }
      );


      var appConfigBuilder = new Conf.ConfigurationBuilder();
      var hostConfiguration = hostConfigBuilder.Build();
                                      // list of IConfigurationSource's
      MyHostingEnvironment hostingEnvironment = new MyHostingEnvironment();
                                      // root directory, etc. host related
      Arr.Dictionary<object, object> Properties = new Arr.Dictionary<object, object>(); 
                                      // for arbitrary metadata
      HostExt.HostBuilderContext hostBuilderContext = 
        new HostExt.HostBuilderContext(Properties);
                                      // Container for reference containing:
                                      //   Properties
                                      //   HostingEnvironment
                                      //   Merged App and Host IConfiguration
      hostBuilderContext.HostingEnvironment = hostingEnvironment;
      hostBuilderContext.Configuration = hostConfigBuilder.Build();

      string appName = hostingEnvironment.ApplicationName; 
                                    // _hostConfiguration[HostDefaults.ApplicationKey]
      string envName = hostingEnvironment.EnvironmentName; 
                                    // _hostConfiguration[HostDefaults.EnvironmentKey]
      string contentRootPath = hostingEnvironment.ContentRootPath; 
                                    // ResolveContentRootPath(
                                    //   _hostConfiguration[HostDefaults.ContentRootKey], 
                                    //   AppContext.BaseDirectory
                                    //  )
      ConfJson.JsonConfigurationSource jsonConfigSrc = new ConfJson.JsonConfigurationSource()
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.Json/src/JsonConfigurationSource.cs
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.Json/src/JsonConfigurationProvider.cs
      {
        FileProvider = null,        // use default one
        Path = "appsettings.json",
        Optional = false,           // file must exists
        ReloadOnChange = false,     // add file watcher and propagate changes
      }; 
      appConfigBuilder.Add(jsonConfigSrc);
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.Abstractions/src/ConfigurationExtensions.cs
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.Json/src/JsonConfigurationExtensions.cs
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.Json/src/JsonConfigurationFileParser.cs
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.Json/src/JsonConfigurationProvider.cs
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.FileExtensions/src/FileConfigurationProvider.cs
                                    // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration/src/ConfigurationProvider.cs
      ConfJson.JsonConfigurationSource jsonEnvedConfigSrc = new ConfJson.JsonConfigurationSource()
      {
        FileProvider = null,        // use default one if file exists
        Path = $"appsettings.{hostBuilderContext.HostingEnvironment.EnvironmentName}.json",
        Optional = false,           // file must exists
        ReloadOnChange = false,     // add file watcher and propagate changes
      };

      /* 
        User secrets:

          https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.UserSecrets/src/PathHelper.cs

        */
      string userSecretsFilePath = Io.Path.Combine(
                                    // "~/.microsoft/usersecrets/my_secrets_secret_id/secrets.json"
        Sys.Environment.GetEnvironmentVariable("HOME"), // home folder
        ".microsoft",               // microsoft configs folder
        "usersecrets",              // secrets of apps
        "my_secrets_secret_id",     // secrets of this app
        "secrets.json"              // file containing secrets
      );

      ConfJson.JsonConfigurationSource secretsConfigSrc = new ConfJson.JsonConfigurationSource()
      {
        FileProvider = null,        // use default one if file exists
        Path = userSecretsFilePath,
        Optional = true,            // file may or may not exists
        ReloadOnChange = false,     // add file watcher and propagate changes
      };

      Conf.FileConfigurationExtensions.SetBasePath(
        appConfigBuilder,
        hostingEnvironment.ContentRootPath
      );
      Conf.ChainedBuilderExtensions.AddConfiguration(
        appConfigBuilder,
        hostConfiguration, 
        shouldDisposeConfiguration: true
      );
      Conf.IConfiguration appConfiguration = appConfigBuilder.Build();
        
      hostBuilderContext.Configuration = appConfiguration; 

      Di.ServiceCollection services = new Di.ServiceCollection();
      Di.ServiceCollectionServiceExtensions
        .AddSingleton<Host.IWebHostEnvironment>(
          services, hostingEnvironment
        );
      Di.ServiceCollectionServiceExtensions
        .AddSingleton<HostExt.IHostEnvironment>(
          services, hostingEnvironment
        );
      Di.ServiceCollectionServiceExtensions
        .AddSingleton<HostExt.HostBuilderContext>(
          services, hostBuilderContext
        );
      Di.ServiceCollectionServiceExtensions
        .AddSingleton<HostExt.IHostApplicationLifetime, HostExtInt.ApplicationLifetime>(
          services
        );
      Di.ServiceCollectionServiceExtensions
        .AddSingleton<HostExt.IHostLifetime, HostExtInt.ConsoleLifetime>(
          services
        );

      Sys.IServiceProvider appServices = null;
      Di.ServiceCollectionServiceExtensions
        .AddSingleton<HostExt.IHost>(
          services,
          implementationFactory: _ =>
          {
            return new MyHost()
            {
              Services = appServices,
              ApplicationLifetime = Di.ServiceProviderServiceExtensions
                .GetRequiredService<HostExt.IHostApplicationLifetime>(appServices),
              Logger = Di.ServiceProviderServiceExtensions
                .GetRequiredService<Log.ILogger<MyHost>>(appServices),
              HostLifetime = Di.ServiceProviderServiceExtensions
                .GetRequiredService<HostExt.IHostLifetime>(appServices),
              Options = Di.ServiceProviderServiceExtensions
                .GetRequiredService<Opt.IOptions<HostExt.HostOptions>>(appServices).Value,

              appConfigBuilder = appConfigBuilder,
              appConfiguration = appConfiguration,
              hostBuilderContext = hostBuilderContext,
              hostConfigBuilder = hostConfigBuilder,
              hostConfiguration = hostConfiguration,

            };
          }
        );
      
      Di.OptionsServiceCollectionExtensions.AddOptions(services);
      Di.OptionsServiceCollectionExtensions.Configure<HostExt.HostOptions>(
        services,
        options => 
        { 
          var timeoutSeconds = hostConfiguration["shutdownTimeoutSeconds"];
          if (
            !string.IsNullOrEmpty(timeoutSeconds)
            && int.TryParse(
              timeoutSeconds, 
              Glob.NumberStyles.None, 
              Glob.CultureInfo.InvariantCulture, 
              out var seconds
            )
          )
          {
            options.ShutdownTimeout = Sys.TimeSpan.FromSeconds(seconds);
          }
        }
      );
      Di.LoggingServiceCollectionExtensions.AddLogging(services);


      Di.IServiceProviderFactory<Di.IServiceCollection> servicesFactory =
        new Di.DefaultServiceProviderFactory();
      Di.IServiceCollection containerBuilder = servicesFactory.CreateBuilder(services);
      appServices = servicesFactory.CreateServiceProvider(containerBuilder);

      
      return (MyHost) Di.ServiceProviderServiceExtensions
        .GetRequiredService<HostExt.IHost>(appServices);
    }

  }
}