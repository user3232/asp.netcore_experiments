using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading; // CancellationToken
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;



namespace SAuth2.HostExperiments
{
  public class MyHost : IHost, IAsyncDisposable
  {
    public ILogger<MyHost> Logger;
    public IHostLifetime HostLifetime;
    public IHostApplicationLifetime ApplicationLifetime;
    public HostOptions Options;
    public IEnumerable<IHostedService> HostedServices;

    public IConfigurationBuilder hostConfigBuilder;
    public IConfigurationBuilder appConfigBuilder;
    public IConfiguration hostConfiguration;
    public HostBuilderContext hostBuilderContext;
    public IConfiguration appConfiguration;
    ServiceCollection serviceCollection;

    public IServiceProvider Services { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
      using var combinedCancellationTokenSource = CancellationTokenSource
        .CreateLinkedTokenSource(
          cancellationToken, 
          ApplicationLifetime.ApplicationStopping
        );
      CancellationToken combinedCancellationToken = combinedCancellationTokenSource.Token;
      await HostLifetime.WaitForStartAsync(combinedCancellationToken).ConfigureAwait(false); 
      combinedCancellationToken.ThrowIfCancellationRequested();
      HostedServices = Services.GetService<IEnumerable<IHostedService>>();     
      foreach (IHostedService hostedService in HostedServices)
      {
        // Fire IHostedService.Start
        await hostedService.StartAsync(combinedCancellationToken).ConfigureAwait(false);

        if (hostedService is BackgroundService backgroundService)
        {
         
        }

      }

      // Fire IHostApplicationLifetime.Started
      // ApplicationLifetime.NotifyStarted();  
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
      using (var cts = new CancellationTokenSource(Options.ShutdownTimeout))
      using (
        var linkedCts = CancellationTokenSource
          .CreateLinkedTokenSource(cts.Token, cancellationToken)
      )
      {
        CancellationToken token = linkedCts.Token;
        // Trigger IHostApplicationLifetime.ApplicationStopping
        ApplicationLifetime.StopApplication();

        IList<Exception> exceptions = new List<Exception>();
        if (HostedServices != null) // Started?
        {
          foreach (IHostedService hostedService in HostedServices.Reverse())
          {
            try
            {
              await hostedService.StopAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
              exceptions.Add(ex);
            }
          }
        }

        // Fire IHostApplicationLifetime.Stopped
        // ApplicationLifetime.NotifyStopped();

        try
        {
          await HostLifetime.StopAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          exceptions.Add(ex);
        }

        if (exceptions.Count > 0)
        {
          var ex = new AggregateException(
            "One or more hosted services failed to stop.", 
            exceptions
          );
          throw ex;
        }
      }
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
      switch (Services)
      {
        case IAsyncDisposable asyncDisposable:
          await asyncDisposable.DisposeAsync().ConfigureAwait(false);
          break;
        case IDisposable disposable:
          disposable.Dispose();
          break;
      }
    }
  }

}