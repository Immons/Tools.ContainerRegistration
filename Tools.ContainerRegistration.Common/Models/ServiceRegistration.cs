using System.Collections.Generic;
using Tools.ContainerRegistration.Common.Generators.Interfaces;

namespace Tools.ContainerRegistration.Common.Models;

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