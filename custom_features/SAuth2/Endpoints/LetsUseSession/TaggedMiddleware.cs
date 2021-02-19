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


namespace SAuth2.Endpoints.LetsUseSession
{

  // public interface ISessionFeature<TTag> : ISessionFeature {}
  // public class SessionFeature<TTag> : SessionFeature, ISessionFeature<TTag> {}
  public class SessionOptions<TTag> : SessionOptions {}
  public interface IAsyncSessionFeature<TTag>
  {
    public Task<ISession> Load(CancellationToken cancellationToken = default(CancellationToken));
    public bool SessionIsNotNull {get;}
  }
  public class AsyncSessionFeature<TTag> : IAsyncSessionFeature<TTag>
  {
    private ISession Session;
    public AsyncSessionFeature(ISession session) {Session = session;}
    public async Task<ISession> Load(
      CancellationToken cancellationToken = default(CancellationToken))
    {
      await Session.LoadAsync(cancellationToken);
      return Session;
    }
    public bool SessionIsNotNull => Session != null;
  }


  /// <summary>
  /// Enables the session state for the application. See also: 
  /// <see cref="https://github.com/dotnet/aspnetcore/blob/master/src/Middleware/Session/src/SessionMiddleware.cs"/>
  /// </summary>
  /// <typeparam name="TTag">Tag type, allows multiple DI injections.</typeparam>
  public class TaggedSessionMiddleware<TTag> 
  {
    private const int SessionKeyLength = 36; // "382c74c3-721d-4f34-80e5-57657b6cbc27"
    private static readonly Func<bool> ReturnTrue = () => true;
    private readonly RequestDelegate _next;
    private readonly SessionOptions _options;
    private readonly ILogger _logger;
    private readonly ISessionStore _sessionStore;
    private readonly IDataProtector _dataProtector;

    /// <summary>
    ///   Creates a new <see cref="SessionMiddleware"/>.
    /// </summary>
    /// <param name="next">
    ///   The <see cref="RequestDelegate"/> representing the
    ///   next middleware in the pipeline.</param>
    /// <param name="loggerFactory">
    ///   The <see cref="ILoggerFactory"/> representing the
    ///   factory that used to create logger instances.
    /// </param>
    /// <param name="dataProtectionProvider">
    ///   The <see cref="IDataProtectionProvider"/> used to protect 
    ///   and verify the cookie.
    /// </param>
    /// <param name="sessionStore">
    ///   The <see cref="ISessionStore"/> representing the session store.
    /// </param>
    /// <param name="options">The session configuration options.</param>
    public TaggedSessionMiddleware(
      RequestDelegate                 next,
      ILoggerFactory                  loggerFactory,
      IDataProtectionProvider         dataProtectionProvider,
      ISessionStore                   sessionStore,
      IOptions<SessionOptions<TTag>>  options)
    {
      if (next == null)
      {
        throw new ArgumentNullException(nameof(next));
      }

      if (loggerFactory == null)
      {
        throw new ArgumentNullException(nameof(loggerFactory));
      }

      if (dataProtectionProvider == null)
      {
        throw new ArgumentNullException(nameof(dataProtectionProvider));
      }

      if (sessionStore == null)
      {
        throw new ArgumentNullException(nameof(sessionStore));
      }

      if (options == null)
      {
        throw new ArgumentNullException(nameof(options));
      }

      _next = next;
      // CHANGED: 
      _logger = loggerFactory.CreateLogger<TaggedSessionMiddleware<TTag>>();
      // CHANGED: 
      _dataProtector = dataProtectionProvider
        .CreateProtector(typeof(TaggedSessionMiddleware<TTag>).FullName);
      _options = options.Value;
      _sessionStore = sessionStore;
    }

    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>
    ///   A <see cref="Task"/> that completes when the 
    ///   middleware has completed processing.
    /// </returns>
    public async Task Invoke(HttpContext context)
    {
      var isNewSessionKey = false;
      Func<bool> tryEstablishSession = ReturnTrue;
      var cookieValue = context.Request.Cookies[_options.Cookie.Name!];
      var sessionKey = CookieProtection.Unprotect(
        protector: _dataProtector, 
        protectedText: cookieValue, 
        logger: _logger);
      
      if (string.IsNullOrWhiteSpace(sessionKey) 
        || sessionKey.Length != SessionKeyLength)
      {
        // No valid cookie, new session.
        var guidBytes = new byte[16];
        RandomNumberGenerator.Fill(guidBytes);
        sessionKey = new Guid(guidBytes).ToString();
        cookieValue = CookieProtection.Protect(_dataProtector, sessionKey);
        var establisher = new SessionEstablisher(context, cookieValue, _options);
        tryEstablishSession = establisher.TryEstablishSession;
        isNewSessionKey = true;
      }

      
      // CHANGED: 
      // var session = _sessionStore.Create(
      //   sessionKey: sessionKey, 
      //   idleTimeout: _options.IdleTimeout, 
      //   ioTimeout: _options.IOTimeout, 
      //   tryEstablishSession: tryEstablishSession, 
      //   isNewSessionKey: isNewSessionKey
      // );
      // var feature = new SessionFeature<TTag>();
      // feature.Session = session;
      // context.Features.Set<ISessionFeature<TTag>>(feature);

      var feature = new AsyncSessionFeature<TTag>(
        session: _sessionStore.Create(
          sessionKey: sessionKey, 
          idleTimeout: _options.IdleTimeout, 
          ioTimeout: _options.IOTimeout, 
          tryEstablishSession: tryEstablishSession, 
          isNewSessionKey: isNewSessionKey
        )
      );
      context.Features.Set<IAsyncSessionFeature<TTag>?>(instance: feature);
      

      try
      {
        await _next(context);
      }
      finally
      {
        context.Features.Set<IAsyncSessionFeature<TTag>?>(null);

        if (feature.SessionIsNotNull)
        {
          try
          {
            var session = await feature.Load();
            // session.
            await session.CommitAsync();
          }
          catch (OperationCanceledException)
          {
            // _logger.SessionCommitCanceled();
            // CHANGED: 
            _logger.LogInformation(
              eventId: new EventId(10, "SessionCommitCanceled"),
              exception: null,
              message: "Error closing the session."
            );
          }
          catch (Exception ex)
          {
            // _logger.ErrorClosingTheSession(ex);
            // _logger.LogError(ex, "...");
            // CHANGED: 
            _logger.LogError(
              eventId: new EventId(1, "ErrorClosingTheSession"),
              exception: ex,
              message: "Error closing the session."
            );
          }
        }
      }
    }

    private class SessionEstablisher
    {
      private readonly HttpContext _context;
      private readonly string _cookieValue;
      private readonly SessionOptions _options;
      private bool _shouldEstablishSession;

      public SessionEstablisher(
        HttpContext context, 
        string cookieValue, 
        SessionOptions options)
      {
        _context = context;
        _cookieValue = cookieValue;
        _options = options;
        context.Response.OnStarting(OnStartingCallback, state: this);
      }

      private static Task OnStartingCallback(object state)
      {
        var establisher = (SessionEstablisher)state;
        if (establisher._shouldEstablishSession)
        {
          establisher.SetCookie();
        }
        return Task.CompletedTask;
      }

      private void SetCookie()
      {
        CookieBuilder? cookieBuilder = _options.Cookie;
        CookieOptions? cookieOptions = cookieBuilder.Build(context: _context);

        var response = _context.Response;
        response.Cookies.Append(
          key:      _options.Cookie.Name!, 
          value:    _cookieValue, 
          options:  cookieOptions);

        var responseHeaders = response.Headers;
        responseHeaders[HeaderNames.CacheControl] = "no-cache,no-store";
        responseHeaders[HeaderNames.Pragma] = "no-cache";
        responseHeaders[HeaderNames.Expires] = "-1";
      }

      // Returns true if the session has already been established, 
      // or if it still can be because the response has not been sent.
      internal bool TryEstablishSession()
      {
        return (_shouldEstablishSession |= !_context.Response.HasStarted);
      }
    }
  }

  internal static class CookieProtection
  {
    internal static string Protect(IDataProtector protector, string data)
    {
      if (protector == null)
      {
        throw new ArgumentNullException(nameof(protector));
      }
      if (string.IsNullOrEmpty(data))
      {
        return data;
      }

      var userData = Encoding.UTF8.GetBytes(data);

      var protectedData = protector.Protect(userData);
      return Convert.ToBase64String(protectedData).TrimEnd('=');
    }

    internal static string Unprotect(
      IDataProtector protector, 
      string? protectedText, 
      ILogger logger)
    {
      // var protectedYoyo = protector
      //   .CreateProtector("idontknow")
      //   .ToTimeLimitedDataProtector()
      //     .Protect(
      //       "yo yo", 
      //       DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10)
      //     );
      try
      {
        if (string.IsNullOrEmpty(protectedText))
        {
          return string.Empty;
        }

        var protectedData = Convert.FromBase64String(Pad(protectedText));
        if (protectedData == null)
        {
          return string.Empty;
        }

        var userData = protector.Unprotect(protectedData);
        if (userData == null)
        {
          return string.Empty;
        }

        return Encoding.UTF8.GetString(userData);
      }
      catch (Exception ex)
      {
        // Log the exception, but do not leak other information
        // logger.ErrorUnprotectingSessionCookie(ex);
        // CHANGED: 
        logger.LogWarning(
          eventId: new EventId(7, "ErrorUnprotectingCookie"),
          exception: ex,
          message: "Error unprotecting the session cookie."
        );
        return string.Empty;
      }
    }

    private static string Pad(string text)
    {
      var padding = 3 - ((text.Length + 3) % 4);
      if (padding == 0)
      {
        return text;
      }
      return text + new string('=', padding);
    }
  }
}
#nullable restore