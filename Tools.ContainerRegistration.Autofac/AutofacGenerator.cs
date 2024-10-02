using System.Text;
using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Autofac;

public class AutofacGenerator : IGenerator
{
    public string Name => "Autofac";
    public string ProviderType => "IContainerContext";
    public string ContainerType => "ContainerBuilder";
    public string Namespace => "Autofac";
    
    public ServiceRegistration GetServiceRegistration() => new AutofacServiceRegistration();
    
    public string GenerateFactoryRegistration(string factoryMethodName, string interfaceTypeName)
    {
        return
            $"builder.Register(context => {factoryMethodName}(typeof({interfaceTypeName}), context))";
    }
    
    public string GenerateSelf()
    {
        return $".AsSelf()";
    }
    
    public string GenerateAs(string interfaceTypeName)
    {
        return $".As<{interfaceTypeName}>()";
    }
    
    public string GenerateSingleInstance()
    {
        return ".SingleInstance()";
    }
    
    public string GenerateInstancePerLifetimeScope()
    {
        return ".InstancePerLifetimeScope()";
    }
    
    public string GenerateAutoActivate()
    {
        return ".AutoActivate()";
    }
    
    public string GenerateRegisterType(string interfaceTypeName)
    {
        return $"builder.RegisterType<{interfaceTypeName}>()";
    }

    public GeneratedServiceRegistrationEntity Generate(ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var registrationLine = new StringBuilder();
        var typeName =
            serviceRegistrationEntity.NamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (serviceRegistrationEntity.FactoryRegistration != null)
        {
            registrationLine.Append(
                GenerateFactoryRegistration(
                    serviceRegistrationEntity.FactoryRegistration.FactoryMethodName,
                    typeName));
        }
        else
        {
            registrationLine.Append(GenerateRegisterType(typeName));
        }

        foreach (var registerAsInterface in serviceRegistrationEntity.RegisterAsInterfaces)
        {
            registrationLine.Append(GenerateAs(registerAsInterface));
        }
        
        if (serviceRegistrationEntity.RegisterAsSelf)
        {
            registrationLine.Append(GenerateSelf());
        }

        if (serviceRegistrationEntity.Scope == Scope.Singleton)
        {
            registrationLine.Append(GenerateSingleInstance());
            if (serviceRegistrationEntity.AutoActivate)
            {
                registrationLine.Append(GenerateAutoActivate());
            }
        }
        else if (serviceRegistrationEntity.Scope == Scope.Scoped)
        {
            registrationLine.Append(GenerateInstancePerLifetimeScope());
        }

        registrationLine.Append(";");

        return new GeneratedServiceRegistrationEntity
        {
            Entity = registrationLine.ToString().Trim()
        };
    }
}