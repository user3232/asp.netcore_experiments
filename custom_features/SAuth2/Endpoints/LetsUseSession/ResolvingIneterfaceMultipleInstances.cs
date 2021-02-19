using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;
using ServiceDescriptor = Microsoft.Extensions.DependencyInjection.ServiceDescriptor;
using ServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;
using IServiceProvider = System.IServiceProvider;
using System.Collections.Generic;

namespace SAuth2.Endpoints.LetsUseSession
{

  public interface IToBeResolved {}
  public class ResolvedA : IToBeResolved {}
  public class ResolvedB : IToBeResolved {}
  public class ResolvedC : IToBeResolved {}
  public interface IToBeResolved<TTag> : IToBeResolved {}
  public class Resolved<TTag> : IToBeResolved<TTag> {}
  public class ResolvedX : IToBeResolved<ResolvedX> {}
  public class ResolvedY : IToBeResolved<ResolvedY> {}
  public class Consumer<TTag> 
  {
    public IToBeResolved<TTag> Dependency;
  }

  public interface IService {}
  public interface IService<TInstanceTag> : IService {}
  public interface ITag1 {}
  public interface ITag2 {}
  public interface IDep1 {}
  public interface IDep1<TInstanceTag> : IDep1 {}
  public interface IDep2 {}
  public interface IDep2<TInstanceTag> : IDep2 {}

  public class Service<TInstanceTag> : IService<TInstanceTag>
  {
    public IDep1 Dep1;
    public IDep2 Dep2;

    // how to have multiple Service's with different
    // deps implementation if Service is get from
    // di container?

    public static IServiceCollection RegisterTwoTagged(
      IServiceCollection services
    )
    {
      services.Add(ServiceDescriptor.Scoped<IService<ITag1>>(
        (IServiceProvider provider) => new Service<ITag1>{
          Dep1 = (IDep1) provider.GetService(typeof(IDep1<ITag2>)),
          Dep2 = (IDep2) provider.GetService(typeof(IDep2<ITag2>))
        }
      ));

      services.Add(ServiceDescriptor.Scoped<IService<ITag2>>(
        (IServiceProvider provider) => new Service<ITag2>{
          Dep1 = (IDep1) provider.GetService(typeof(IDep1<ITag1>)),
          Dep2 = (IDep2) provider.GetService(typeof(IDep2<ITag2>))
        }
      ));

      return services;
    }
  }

  public static class ResolvingInterfaceMultipleInstances
  {

    public static IServiceCollection AddServices(
      IServiceCollection services
    )
    {
      services.Add(ServiceDescriptor.Describe(
        serviceType: typeof (IToBeResolved),
        implementationFactory: (IServiceProvider provider) => new ResolvedA(),
        lifetime: ServiceLifetime.Scoped
      ));
      services.Add(ServiceDescriptor.Scoped<IToBeResolved, ResolvedB>());
      services.Add(ServiceDescriptor.Scoped(
        service: typeof (IToBeResolved), 
        implementationType: typeof (ResolvedC)
      ));
      // provider.GetService(typeof(IToBeResolved)) will return last of above
      // provider.GetService(typeof(IEnumerable<IToBeResolved>)) will return all above


      services.Add(ServiceDescriptor.Scoped(
        typeof(IToBeResolved<>),
        typeof(Resolved<>)
      )); 
      // above will be overwritten by:
      //    IToBeResolved<ResolvedX> 
      //    and IToBeResolved<ResolvedY>
      services.Add(ServiceDescriptor.Scoped(
        typeof(IToBeResolved<ResolvedX>),
        typeof(ResolvedX)
      ));
      services.Add(
        ServiceDescriptor.Scoped<IToBeResolved<ResolvedY>, ResolvedY>()
      );
      // provider.GetService(typeof(IEnumerable<IToBeResolved>))
      // does it return [ResolvedA, ResolvedB, ResolvedC]
      // or [ResolvedA, ResolvedB, ResolvedC] + [ResolvedX, ResolvedY]
      // or even [every possible type Resolved<...>]
      // What will return provider.GetService(typeof(IEnumerable<>)) ??




      services.Add(ServiceDescriptor.Scoped<ResolvedX, ResolvedX>());
      services.Add(ServiceDescriptor.Scoped<ResolvedY, ResolvedY>());
      return services;
    }

    public static void RetrivingParticularImplementation(
      IServiceProvider provider
    )
    {
      var iss = (IEnumerable<IToBeResolved>) provider.
        GetService(typeof(IEnumerable<IToBeResolved>));


    }

  }


}