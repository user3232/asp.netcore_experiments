using Deps = Microsoft.Extensions.DependencyInjection;
using DepsAdd = Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions;
using CacheServEx = Microsoft.Extensions.DependencyInjection.MemoryCacheServiceCollectionExtensions;
using Cache = Microsoft.Extensions.Caching.Distributed;
using SessionServEx = Microsoft.Extensions.DependencyInjection.SessionServiceCollectionExtensions;
using AppBuild = Microsoft.AspNetCore.Builder;
using SessionMidEx = Microsoft.AspNetCore.Builder.SessionMiddlewareExtensions;
using Session = Microsoft.AspNetCore.Session;
using Log = Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;
using CancellationToken = System.Threading.CancellationToken;
using ISession = Microsoft.AspNetCore.Http.ISession;
using System.Collections.Generic;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;
using Http = Microsoft.AspNetCore.Http;
using Container = System.Collections.Generic;
using Json = System.Text.Json.JsonSerializer;


// See Asp.net core session middleware source code:
// https://github.com/dotnet/aspnetcore/blob/master/src/Middleware/Session/src/SessionMiddleware.cs
/* 

  See Asp.net core session middleware source code:

  https://github.com/dotnet/aspnetcore/blob/master/src/Middleware/Session/src/SessionMiddleware.cs


  Cookie is:

  * key-value pair + optionally:

    * domain, 
    * path, 
    * expiration 
    * and access settings

  There may be multiple cookies for the same site 
  (https://stackoverflow.com/questions/1131867/can-one-domain-have-multiple-cookies):

  * Browser maximum cookie count are:

    * Firefox 3.0: 50
    * Opera 9: 30
    * Internet Explorer 7: 50

  * All cookies (with matching domain) are send to server

  * This is useful because one don't need to serialize all data
    in one cookie.

  

  ********************************************************************

  Create multiple sessions with different names:

    * Store ISession(s) in httpContext features
    * use it
    * realise as middleware

    * create endpoint using those middlewares

    * create cookie binary serializer 
      (based on DistributedSession.Serialize)

*/
namespace SAuth2.Endpoints.LetsUseSession
{

  public static class SessEndp
  {

    public static async Task Endpoint(Http.HttpContext httpContext)
    {
      await httpContext.Session.LoadAsync();

      // set some session values to identify user
      // check session values

      // httpContext.Features.Set<MySession1>(mySession1);
      
      var (u, x) = PossiblyAddSessionUser(httpContext.Session.Id, null);
      var userData = GetUserData(httpContext.Session.Id);
      await Task.CompletedTask;
    }

    public static (bool added, Container.HashSet<string> ids) PossiblyAddSessionUser(
      string id, 
      Container.HashSet<string> ids
    ) => (added: ids.Add(id), ids: ids);
    
    public static Dictionary<string,string> GetUserData(string id) => null;





    #region  Session key-values setting/getting

    public static void SetSessionEntry<T>(Http.HttpContext httpContext, string key, T value)
    {
      Http.SessionExtensions.SetString(httpContext.Session, key, Json.Serialize(value));
    }
    public static void SetSessionEntry(Http.HttpContext httpContext, string key, string value) 
    {
      Http.SessionExtensions.SetString(httpContext.Session, key, value);
    }
    public static void SetSessionEntry(Http.HttpContext httpContext, string key, int value) 
    {
      Http.SessionExtensions.SetInt32(httpContext.Session, key, value);
    }
    public static T GetSessionEntry<T>(Http.HttpContext httpContext, string key)
    {
      var value = Http.SessionExtensions.GetString(httpContext.Session,key);
      return value == null ? default : Json.Deserialize<T>(value);
    }
    public static string GetSessionString(Http.HttpContext httpContext, string key) 
      => Http.SessionExtensions.GetString(httpContext.Session,key);
    public static int? GetSessionInt(Http.HttpContext httpContext, string key) 
      => Http.SessionExtensions.GetInt32(httpContext.Session,key);

    #endregion






    public static void ConfigureServices(
      Deps.IServiceCollection services
    )
    {
      CacheServEx.AddDistributedMemoryCache(
        services: services,
        setupAction: cacheOpts => 
        {
          cacheOpts.SizeLimit = cacheOpts.SizeLimit;
        }
      );
      SessionServEx.AddSession(
        services: services,
        configure: sessOpts =>
        {
          // time after which cookie content in cache will be abandoned.
          sessOpts.IdleTimeout = System.TimeSpan.FromMinutes(5);
          sessOpts.Cookie.HttpOnly = true;
          sessOpts.Cookie.Name = "my.sess.experiments.session";

          // below means that user must accept cookies!!!!
          // if it does not service will be not available
          sessOpts.Cookie.IsEssential = true; 
        }
      );
    }


    public static void ConfigureServicesWithAsyncSessionService(
      Deps.IServiceCollection services
    )
    {
      services.Add(
        Deps.ServiceDescriptor.Scoped(
          service: typeof (Session.ISessionStore), 
          implementationType: typeof (AsyncRequiredSessionFactory)
        )
      );
      services.Add(
        Deps.ServiceDescriptor.Scoped<ISession, AsyncRequiredSession>()
      );
      // DepsAdd.AddScoped<AsyncRequiredSessionFactory>(services);
      // DepsAdd.AddScoped<AsyncRequiredSession>(services);
    }


    public static void ConfigureApp(
      AppBuild.IApplicationBuilder appBuilder
    )
    {
      SessionMidEx.UseSession(app: appBuilder);
    }





    public class AsyncRequiredSessionFactory : Session.ISessionStore
    {
      private Cache.IDistributedCache _cache;
      private Log.ILoggerFactory _loggerFactory;

      public AsyncRequiredSessionFactory(
        Cache.IDistributedCache cache, 
        Log.ILoggerFactory loggerFactory
      ) 
      {
        _cache = cache; 
        _loggerFactory = loggerFactory;
      }

      public ISession Create(
        string                  sessionKey, 
        System.TimeSpan         idleTimeout, 
        System.TimeSpan         ioTimeout, 
        System.Func<bool>       tryEstablishSession, 
        bool                    isNewSessionKey
      )
      {
        return new AsyncRequiredSession(_cache, sessionKey, idleTimeout, ioTimeout, 
          tryEstablishSession, _loggerFactory, isNewSessionKey);
      }
    }

    public class AsyncRequiredSession : ISession
    {
      
      private bool _loadedAsyncCalled = default;
      private Session.DistributedSession _session;

      public string Id => ((ISession)_session).Id;
      public bool IsAvailable => ((ISession)_session).IsAvailable;
      public IEnumerable<string> Keys => ((ISession)_session).Keys;

      public AsyncRequiredSession(
        Cache.IDistributedCache cache, 
        string                  sessionKey, 
        System.TimeSpan         idleTimeout, 
        System.TimeSpan         ioTimeout, 
        System.Func<bool>       tryEstablishSession, 
        Log.ILoggerFactory      loggerFactory, 
        bool                    isNewSessionKey
      )
      {
        _session = new Session.DistributedSession(
          cache, sessionKey, idleTimeout, ioTimeout, 
          tryEstablishSession, loggerFactory, isNewSessionKey
        );
        _loadedAsyncCalled = false;
      }

      public void Clear()
      {
        ((ISession)_session).Clear();
      }

      public Task CommitAsync(CancellationToken cancellationToken = default)
      {
        return ((ISession)_session).CommitAsync(cancellationToken);
      }



      public async Task LoadAsync(
        CancellationToken cancellationToken = default
      )
      {
        _loadedAsyncCalled = true;
        await ((ISession)_session).LoadAsync();
      }

      public class NotLoadedAsyncException : System.Exception 
      {
        public NotLoadedAsyncException(string method) : base(
          "For asyncronous sessions, ISession.LoadAsync"
           + $" should be called before ISession.{method} ."
           + "For details see:"
           + " https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-5.0#load-session-state-asynchronously ."
        ) { }
      }

      public void Remove(string key) 
      {
        if(_loadedAsyncCalled == false)
          throw new NotLoadedAsyncException("Remove");
        ((ISession)_session).Remove(key);
      }
      public void Set(string key, byte[] value)
      {
        if(_loadedAsyncCalled == false)
          throw new NotLoadedAsyncException("Set");
        ((ISession)_session).Set(key, value);
      }
      public bool TryGetValue(string key, out byte[] value)
      {
        if(_loadedAsyncCalled == false)
          throw new NotLoadedAsyncException("TryGetValue");
        return ((ISession)_session).TryGetValue(key, out value);
      }
      
    }

  }
}