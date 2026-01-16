using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Tools.ContainerRegistration.Common.Models;

public class ServiceRegistrationEntity
{
    public ServiceRegistrationEntity(INamedTypeSymbol namedTypeSymbol)
    {
        NamedTypeSymbol = namedTypeSymbol;
    }

    public List<string> RegisterAsInterfaces { get; } = new List<string>();
    public INamedTypeSymbol NamedTypeSymbol { get; }
    public Scope Scope { get; set; }
    public bool RegisterAsSelf { get; set; }
    public bool AutoActivate { get; set; }
    public FactoryRegistrationEntity FactoryRegistration { get; set; }

    /// <summary>
    /// List of IInjector dependencies that need to be injected via OnActivating.
    /// Each entry contains the fully qualified IInjector interface name and the dependency type name.
    /// </summary>
    public List<InjectorDependency> InjectorDependencies { get; } = new List<InjectorDependency>();

    /// <summary>
    /// List of OnActivated callbacks to invoke after the service is resolved.
    /// </summary>
    public List<OnActivatedCallback> OnActivatedCallbacks { get; } = new List<OnActivatedCallback>();
}

public class OnActivatedCallback
{
    /// <summary>
    /// Fully qualified name of the target type containing the static method.
    /// Null if this is an instance method call.
    /// </summary>
    public string TargetTypeFullName { get; set; }

    /// <summary>
    /// Name of the method to call.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// True if this is an instance method on the resolved service, false if static method.
    /// </summary>
    public bool IsInstanceMethod { get; set; }
}

public class InjectorDependency
{
    /// <summary>
    /// Fully qualified name of IInjector&lt;T&gt; interface, e.g. "global::MyNamespace.IInjector&lt;global::MyNamespace.IService&gt;"
    /// </summary>
    public string InjectorInterfaceFullName { get; set; }

    /// <summary>
    /// Fully qualified name of the dependency type T, e.g. "global::MyNamespace.IService"
    /// </summary>
    public string DependencyTypeFullName { get; set; }
}