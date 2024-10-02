using Microsoft.Extensions.DependencyInjection;

namespace Tools.ContainerRegistration.Microsoft.Extensions;

public static class ServiceCollectionExtension
{

    /// <summary>
    /// Adds a singleton service of the type specified in TBase with a factory based on the registered type T that has been specified in implementation factory to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="T">The registered type</typeparam>
    /// <typeparam name="TBase">The type that T is derived from, can be the base class or base interface.</typeparam>
    /// <param name="services">The services.</param>
    /// <returns>the IServiceCollection used to register the interface with.</returns>
    public static IServiceCollection ReUseSingleton<T, TBase>(this IServiceCollection services)
        where T : TBase
        where TBase : class
    {
        services.AddSingleton<TBase>(a => a.GetRequiredService<T>());
        return services;
    }
}