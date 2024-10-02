using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Common.Generators.Interfaces;

namespace Tools.ContainerRegistration.Common.Models;

public class GeneratedServiceRegistrationEntity
{
    public string Entity { get; set; }
}

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
}

public abstract class ServiceRegistration
{
    public abstract string Build(IGenerator generator);
    public string ContainerName { get; set; }
    public string ContainerType { get; set; }
    public string ProviderType { get; set; }
    public string Namespace { get; set; }
    public List<string> Usings { get; set; } = new List<string>();
    public List<ServiceRegistrationEntity> Entities { get; set; } = new List<ServiceRegistrationEntity>();
}

public enum Scope
{
    Transient,
    Scoped,
    Singleton,
}