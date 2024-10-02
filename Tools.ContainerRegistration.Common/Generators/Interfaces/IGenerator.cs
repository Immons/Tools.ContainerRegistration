using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Common.Generators.Interfaces;

public interface IGenerator
{
    // string GenerateFactoryRegistration(string factoryMethodName, string interfaceTypeName);
    // string GenerateSelf(INamedTypeSymbol type);
    // string GenerateAs(string interfaceTypeName);
    // string GenerateSingleInstance(string interfaceTypeName);
    // string GenerateAutoActivate(string interfaceTypeName);
    // string GenerateRegisterType(string interfaceTypeName);
    string GetContainerType();
    string GetNamespace();
    string Generate(ServiceRegistrationEntity serviceRegistrationEntity);
    string Name { get; }
}