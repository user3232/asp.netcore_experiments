#nullable enable
using NsHost = Microsoft.AspNetCore.Hosting;
using NsHostExts = Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;


namespace SAuth2
{
  public class Program
  {
    public static void Main(string[] args)
    {
      NsHostExts.IHostBuilder hostBuilder = CreateHostBuilder(args: args);
      NsHostExts.IHost host = hostBuilder.Build(); 
      
      NsHostExts.HostingAbstractionsHostExtensions.Run(host: host);
    }

    public static NsHostExts.IHostBuilder CreateHostBuilder(string[] args)
    {
      
      NsHostExts.IHostBuilder hostBuilder = 
        NsHostExts.Host.CreateDefaultBuilder(args: args);

      NsHostExts.GenericHostBuilderExtensions.ConfigureWebHostDefaults(
        builder: hostBuilder,
        configure: (NsHost.IWebHostBuilder hostWebHostBuilder) =>
        {

          // hostWebHostBuilder._builder = hostBuilder
          NsHost.WebHostBuilderExtensions.UseStartup<Startup>(
            hostBuilder: hostWebHostBuilder
          );

          // var hostAddress = hostWebHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);
          return;
        }
      );
      
      return hostBuilder;
    }
  }
}
#nullable restore

/* 

  Overview what is going on in:

  * CreateDefaultBuilder
  * ConfigureWebHostDefaults
  * hostBuilder.Build()
  * HostingAbstractionsHostExtensions.Run()

  can be found at:
  * https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0#default-builder-settings
  * https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-5.0
  * https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#default-configuration


*/

/* 
  Configure big configuration:

    NsHostExts.IHostBuilder hostBuilder = 
      NsHostExts.Host.CreateDefaultBuilder(args: args);

    NsHostExts.GenericHostBuilderExtensions.ConfigureWebHostDefaults(
      builder: hostBuilder,
      configure: (NsHost.IWebHostBuilder hostWebHostBuilder) =>
      {
        // hostWebHostBuilder._builder = hostBuilder
        NsHost.WebHostBuilderExtensions.UseStartup<Startup>(
          hostBuilder: hostWebHostBuilder
        );
        return;
      }
    );
  
    NsHostExts.IHost host = hostBuilder.Build(); // -> IWebHost -> 
    // IHost.Services
    // ((IWebHost)IHost).Services
    // ((IWebHost)IHost).ServerFeatures

  
  Run configured tasks and loops:

    NsHostExts.HostingAbstractionsHostExtensions.Run(host: host);



  In details:

    ConfigureWebHostDefaults overview (gigant mutations and cross globals):
  
    -> GenericHostBuilderExtensions.ConfigureWebHostDefaults(IHostBuilder, string[])  
      -> GenericHostWebHostBuilderExtensions.ConfigureWebHost
          -> new GenericWebHostBuilder(...)
            -> services.TryAddSingleton<IHttpContextFactory,        ...>();
            -> services.TryAddScoped   <IMiddlewareFactory,         ...>();
            -> services.TryAddSingleton<IApplicationBuilderFactory, ...>();
          -> WebHost.ConfigureWebDefaults
            -> UseKestrel
                -> services.AddSingleton<IServer, Kestrel>()
                -> add ServerFeatures
            -> UseRouting
            -> ForwardedHeadersStartupFilter
            -> ...
          -> WebHostBuilderExtensions.UseStartup
            -> fire Startup.Configure
            -> fire Startup.ConfigureServices
          -> services.AddHostedService<GenericWebHostService>()

    NsHostExts.HostingAbstractionsHostExtensions.Run(host: host);

    Fires hosted service(s) (GenericWebHostService):
      hostedServices = services.Get<IHostedService>()
      hostedServices.map(hServ => hServ.Start())
    GenericWebHostService:
      runs app. 


    Now hosted services (GenericWebHostService) are running!
    What are they doing???...
    
    
    GenericWebHostService is dependency injected with:
    
      * IServer                        Server
      * IHttpContextFactory            HttpContextFactory
      * IApplicationBuilderFactory     ApplicationBuilderFactory
      * IConfiguration                 Configuration
      * IWebHostEnvironment            HostingEnvironment 
      * GenericWebHostServiceOptions   Options:
        * WebHostOptions                  WebHostOptions
          * string                          ApplicationName
          * string                          WebRoot
          * IReadOnlyList<string>           HostingStartupAssemblies
          * ...                             ...
        * Action<IApplicationBuilder>     ConfigureApplication
    
    
    GenericWebHostService can be started and doing so it will:
    
      * Get server (listening) addresses from Configuration  
        and apply them to server if server supports this feature
        (IServerAddressesFeature)

      * Create application builder:
        ```
        appBuilder = ApplicationBuilderFactory
          .CreateBuilder(Server.Features);

        IApplicationBuilder ApplicationBuilderFactory.CreateBuilder
        (
          IFeatureCollection serverFeatures
        )
        {
          return new ApplicationBuilder(_serviceProvider, serverFeatures);
        }
        ```

        * ApplicationBuilder contains:
          * Middleware:
            ```
              List<Func<RequestDelegate, RequestDelegate>> _components = new();
            ```
          * Middleware communication channel when defining middleware:
            ```
              Dictionary<string, object?> Properties;
            ```

        * ApplicationBuilder have (Dependency injected) constructors taking:
          * IServiceProvider ApplicationServices
          * (optionally) IFeatureCollection named server (but should be IServer.Features)

        * ApplicationBuilder exposes:
          * Properties (Dictionary<string, object?>)
            * IServiceProvider ApplicationServices (via Porperties[ApplicationServicesKey])
            * IServiceProvider ServerFeatures (via Porperties[ServerFeaturesKey])
            ```
              IServiceProvider GetApplicationServices() => 
                Properties.TryGetValue("application.Services", var value) 
                  ? (IServiceProvider) value 
                  : null;
              IFeatureCollection GetServerFeatures() =>
                Properties.TryGetValue("server.Features", var value) 
                  ? (IFeatureCollection) value 
                  : null;
            ```

        * ApplicationBuilder can build application:
          ```
            public RequestDelegate Build()
            {
              HttpContext NoEndpointReqHandler(HttpContext context) =>
              {
                // If we reach the end of the pipeline, 
                // but endpoint is defined (context.GetEndpoint() != null), 
                // then something unexpected has happened.
                // This could happen if user code sets an endpoint, 
                // but they forgot to add the UseEndpoint middleware.

                var endpoint = context.GetEndpoint();
                var endpointRequestDelegate = endpoint?.RequestDelegate;
                if (endpointRequestDelegate != null)
                {
                  var message =
                      $"The request reached the end of the pipeline"
                    + $" without executing the endpoint:"
                    + $" '{endpoint!.DisplayName}'." 
                    + $" Please register the EndpointMiddleware using"
                    + $" '{nameof(IApplicationBuilder)}.UseEndpoints(...)'" 
                    + $" if using routing.";
                  throw new InvalidOperationException(message);
                }

                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
              };

              RequestDelegate app = NoEndpointReqHandler; 
                                          // this is initial value
                                          // used as last delegate 
                                          // in delegate building by
                                          // functions chaining
              for (var c = _components.Count - 1; c >= 0; c--)
              {
                app = _components[c](app);
              }

              return app;
            }
          ```
      * Apply StartupFilters to Options.ConfigureApplication:
        ```
        var configure = Options.ConfigureApplication;
        foreach (var filter in StartupFilters.Reverse()) 
        {
          configure = filter.Configure(configure);
        }
        ```

      * Apply filtered configurator to appBuilder:
        ```
        configure(appBuilder)
        ```

      * Build request pipeline :
        ```
        var application = appBuilder.Build();
        ```

      * Create http application. (It is passive component,
        it does not run on itself. It needs server features
        to be executed).
        ```
        var httpApplication = new HostingApplication (
          application, 
          Logger,
          DiagnosticListener,
          HttpContextFactory
        );
        ```

        * HostingApplication is functionality packed into RequestDelegate
          and HttpContextFactrory marshalling communication with server.
          HostingApplication have:
          * RequestDelegate            _application;
          * IHttpContextFactory?       _httpContextFactory;
          * DefaultHttpContextFactory? _defaultHttpContextFactory;

        * HostingApplication HttpContextFactory is for producing
          app specific HttpContext based on ServerFeatures
          which must be provided

        * HostingApplication.ProcessRequestAsync applies
          _application to produced HttpContext, this way
          request is executed.

      * Start server:
        ```
        await Server.StartAsync(httpApplication, cancellationToken);
        // -> KestrelServerImpl.StartAsync(httpApplication, cancellationToken)
        ```
        and this starts the server... but what have (Kestrel) server?

        * KestrelServerImpl have:

          * ServerAddressesFeature _serverAddresses
          * TransportManager _transportManager:
            * IConnectionListenerFactory? _transportFactory (SocketTransportFactory)
            * ServiceContext _serviceContext
          * IConnectionListenerFactory? _transportFactory
          * ServiceContext ServiceContext
            * parsing urls see: https://github.com/dotnet/aspnetcore/blob/6b95e58bb4d113d210e519b99f79365b8a5dbb19/src/Servers/Kestrel/Core/src/Internal/Infrastructure/HttpCharacters.cs#L9
            * PipeScheduler Scheduler
            * IHttpParser<Http1ParsingHandler> HttpParser
            * ISystemClock SystemClock
            * DateHeaderValueManager DateHeaderValueManager
            * ConnectionManager ConnectionManager
            * Heartbeat Heartbeat
            * KestrelServerOptions ServerOptions

        * KestrelServerImpl.StartAsync(httpApplication, cancellationToken)
          
          * ServiceContext.Heartbeat?.Start();

            Heartbeat have timer that runs in loop IHartbeatHandler's
            HeartbeatManager : IHartbeatHandler, ISystemClock have:
            * ConnectionManager _connectionManager
            * Action<KestrelConnection> _walkCallback = KestrelConnection.TickHeartbeat()
              * KestrelConnection is IConnectionHeartbeatFeature which allows adding
                heartbeat handler functions, those function takes state which
                is preserved between calls. If dead situation is detected (by measuring
                clock ticks, connection can be killed).

          * AddressBindContext = new AddressBindContext(_serverAddresses, Options, Trace, OnBind);
            AddressBindContext is created, it have:

            * ServerAddressesFeature serverAddressesFeature,
            * KestrelServerOptions serverOptions,
            * ILogger logger,
            * Func<ListenOptions, Task> createBinding = Task OnBind(ListenOptions options)
              OnBind is defined inline when KestrelServerImpl.StartAsync
              OnBind takes as argument ListenOptions which allows parametrizing connection
              middleware (ListenOptions : IConnectionBuilder)

              * OnBind calls listenOptions.UseHttp3Server(ServiceContext, application, options.Protocols);
                * which adds http connection middleware:

                  new HttpConnectionMiddleware<TContext>(serviceContext, application, protocols);

                * then connection middleware is build:
                  
                  var connectionDelegate = options.Build();

                * finally socket binding is created:

                  await _transportManager.BindAsync(listenOptions.EndPoint, connectionDelegate, listenOptions.EndpointConfig)

                  TransportManager.BindAsync:
                  * produces transport channel using factory (SocketTransportFactory)
                  
                    var transport = await _transportFactory.BindAsync(endPoint).ConfigureAwait(false);
                    
                    * and this creates socket connection listener: 
                    
                      var transport = new SocketConnectionListener(endpoint, _options, _trace);
                      transport.Bind();
                      return new ValueTask<IConnectionListener>(transport);
                  * and starts accept loop:

                    StartAcceptLoop(
                      new GenericConnectionListener(transport), 
                      c => connectionDelegate(c), 
                      endpointConfig
                      );
                  
                  * finally returns transport channel associated endpoint:

                    return transport.EndPoint;
            
            * AddressBindContext is used to run AddressBinder.BindAsync static method
              which binds to address configured by listen options:

              AddressBinder.BindAsync(Options.ListenOptions, AddressBindContext!)
                  
              internally it uses AddressBindContext OnBind
            
            * That is it. Socket is listening and doing what has been configured.


      * Print logs: 
        * server addresses,
        * hosting startup assemblies,
        * startup assemblies errors

      * That's it.


*/


/* 
  References:

  * https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Hosting/src/Host.cs
  * https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Hosting/src/HostBuilder.cs
  * https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration/src/ConfigurationBuilder.cs
  * https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Hosting/src/HostBuilder.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/DefaultBuilder/src/GenericHostBuilderExtensions.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/GenericHostWebHostBuilderExtensions.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/DefaultBuilder/src/WebHost.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/WebHostBuilderExtensions.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/GenericHost/GenericWebHostBuilder.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/WebHostBuilder.cs
  * https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Hosting/src/Internal/Host.cs
  * https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Hosting.Abstractions/src/HostingAbstractionsHostExtensions.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/WebHostBuilder.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Http/Http.Features/src/FeatureCollection.cs


  * https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/GenericHost/GenericWebHostedService.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/Builder/ApplicationBuilderFactory.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Http/Http/src/Builder/ApplicationBuilder.cs
  * https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/Internal/HostingApplication.cs

*/
  




  

  

  




