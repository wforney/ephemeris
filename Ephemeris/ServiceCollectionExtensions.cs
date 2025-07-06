using Microsoft.Extensions.DependencyInjection;

namespace Ephemeris;

/// <summary>
/// Interface for scoped services in the Ephemeris library.
/// </summary>
public interface IScopedService;

/// <summary>
/// Interface for singleton services in the Ephemeris library.
/// </summary>
public interface ISingletonService;

/// <summary>
/// Interface for transient services in the Ephemeris library.
/// </summary>
public interface ITransientService;

/// <summary>
/// Extension methods for registering Ephemeris services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Ephemeris services in the provided service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to add the services to.
    /// </param>
    /// <returns>
    /// The updated service collection with the Ephemeris services registered.
    /// </returns>
    public static IServiceCollection AddEphemerisServices(this IServiceCollection services)
    {
        return services.Scan(
            scan =>
                scan
                    .FromAssemblyOf<ITransientService>()
                        .AddClasses(classes => classes.AssignableTo<IScopedService>())
                            .As<IScopedService>()
                            .WithScopedLifetime()
                        .AddClasses(classes => classes.AssignableTo(typeof(ISingletonService)))
                            .AsImplementedInterfaces()
                            .WithSingletonLifetime()
                        .AddClasses(classes => classes.AssignableTo<ITransientService>())
                            .AsImplementedInterfaces()
                            .WithTransientLifetime());
    }
}
