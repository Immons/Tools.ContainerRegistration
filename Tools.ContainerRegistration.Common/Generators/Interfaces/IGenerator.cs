using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Common.Generators.Interfaces;

public interface IGenerator
{
    string ContainerType { get; }
    string Namespace { get; }
    GeneratedServiceRegistrationEntity Generate(ServiceRegistrationEntity serviceRegistrationEntity);
    string Name { get; }
    string ProviderType { get; }
    ServiceRegistration GetServiceRegistration();
}