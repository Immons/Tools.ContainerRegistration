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
}