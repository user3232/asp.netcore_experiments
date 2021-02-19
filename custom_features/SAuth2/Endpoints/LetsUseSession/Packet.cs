#nullable enable

using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace SAuth2.Endpoints.LetsUseSession
{

  public static class Packet
  {


    // Register object factories
    // (factory is also type itself)
    // (keys are (base) types (interfaces))
    public static IServiceCollection HowToRegisterGettingTaggedSessionRequiredDataAndApi(
      this IServiceCollection services, 
      Action<SessionOptions>? configure = null, 
      Action<MemoryDistributedCacheOptions>? setupAction = null
    )
    {
      if (services == null)
      {
        throw new ArgumentNullException(nameof(services));
      }

      /* 
        Add options factiories to dependency injection container:
          services.AddOptions();
            https://github.com/aspnet/Options/blob/95b968014f16c3e1b61d1b830022484b9d0db187/src/Microsoft.Extensions.Options/OptionsServiceCollectionExtensions.cs#L23
      */
      services.TryAdd(ServiceDescriptor.Singleton(
        typeof(IOptions<>), 
        typeof(OptionsManager<>)
      ));
      services.TryAdd(ServiceDescriptor.Scoped(
        typeof(IOptionsSnapshot<>), 
        typeof(OptionsManager<>)
      ));
      services.TryAdd(ServiceDescriptor.Singleton(
        typeof(IOptionsMonitor<>), 
        typeof(OptionsMonitor<>)
      ));
      services.TryAdd(ServiceDescriptor.Transient(
        typeof(IOptionsFactory<>), 
        typeof(OptionsFactory<>)
      ));
      services.TryAdd(ServiceDescriptor.Singleton(
        typeof(IOptionsMonitorCache<>), 
        typeof(OptionsCache<>)
      ));
      

      /* 
        Add IDistributedMemoryCache:
          services.AddDistributedMemoryCache();
            https://github.com/aspnet/Caching/blob/9db2c381b19ff2aeb8d6783f145c3c41e1529b78/src/Microsoft.Extensions.Caching.Memory/MemoryCacheServiceCollectionExtensions.cs#L76
      */
      services.TryAdd(
        ServiceDescriptor.Singleton<IDistributedCache, MemoryDistributedCache>()
      );
      if (setupAction != null)
      {
        services.Configure(setupAction);
      }
      
      /* 
        Add ISessionStore:
          services.AddSession()
            https://github.com/aspnet/Session/blob/master/src/Microsoft.AspNetCore.Session/SessionServiceCollectionExtensions.cs
      */
      if (configure != null)
      {
        services.Configure(configure);
      }
      services.TryAddTransient<ISessionStore, DistributedSessionStore>();
      
      /* 
        Add IDataProtection:
            https://github.com/dotnet/aspnetcore/blob/master/src/DataProtection/DataProtection/src/DataProtectionServiceCollectionExtensions.cs
      */
      services.AddDataProtection();

      return services;
    }


    // Register in pipeline
    // (no keys, placement in pipeline)
    // (but consumers can be get by key if object is registered in features collection)
    // (pipeline fragment (middleware) is also consumer)
    public static IApplicationBuilder HowToAddTaggedSessionToPipeline(
      this IApplicationBuilder app, 
      SessionOptions? options = null)
    {
      if (app == default(IApplicationBuilder))
      {
        throw new ArgumentNullException(nameof(app));
      }

      if (options == default(SessionOptions))
      {
        return app.UseMiddleware<SessionMiddleware>();
      }
      else
      {
        return app.UseMiddleware<SessionMiddleware>(Options.Create(options));
      }
    }


    // Consume
    // (Can also set (type, object) in features collection)
    // (HttpContext.Features probably are reset by every request)
    public static async Task<ISession> HowToConsumeTaggedSessionWork<TTag>(
      HttpContext httpContext
    )
    {
      var asyncSessFeat = httpContext.Features.Get<IAsyncSessionFeature<TTag>>();
      return await asyncSessFeat.Load();
    }


  }


}

#nullable restore
