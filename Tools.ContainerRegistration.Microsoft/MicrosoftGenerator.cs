using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Microsoft;

public class MicrosoftGenerator : IGenerator
{
    public string Name => "ServiceCollection";
    public string ProviderType => "IServiceProvider";
    public string ContainerType => $"IServiceCollection";
    public string Namespace => "Microsoft.Extensions.DependencyInjection";
    
    public ServiceRegistration GetServiceRegistration() => new MicrosoftServiceRegistration();

    private string GenerateFactoryRegistration(string factoryMethodName, string interfaceTypeName, Scope scope)
    {
        if (scope == Scope.Singleton)
            return $"builder.AddSingleton(typeof({interfaceTypeName}), provider => {factoryMethodName}(typeof({interfaceTypeName}), provider));";
        if (scope == Scope.Scoped)
            return $"builder.AddScoped(typeof({interfaceTypeName}), provider => {factoryMethodName}(typeof({interfaceTypeName}), provider));";

        return $"builder.AddTransient(typeof({interfaceTypeName}), provider => {factoryMethodName}(typeof({interfaceTypeName}), provider));";
    }

    private string GenerateSelf(
        string typeName,
        Scope scope)
    {
        if (scope == Scope.Singleton)
            return GenerateSingleInstance(typeName);
        if (scope == Scope.Scoped)
            return GenerateScopedInstance(typeName);
        else
            return GenerateTransientInstance(typeName);
    }
    
    public string GenerateAs(string interfaceTypeName, bool singleton)
    {
        if (singleton)
            return GenerateSingleInstance(interfaceTypeName);
        else
            return GenerateTransientInstance(interfaceTypeName);
    }
    
    public string GenerateAs(string typeName, string interfaceTypeName, bool singleton)
    {
        if (singleton)
            return GenerateSingleInstance(typeName, interfaceTypeName);
        else
            return GenerateTransientInstance(typeName, interfaceTypeName);
    }

    public string GenerateSingleInstance(string typeName)
    {
        return $"builder.AddSingleton<{typeName}>();";
    }
    
    public string GenerateSingleInstance(string typeName, string interfaceTypeName)
    {
        return $"builder.AddSingleton<{interfaceTypeName}, {typeName}>();";
    }

    public string GenerateScopedInstance(string typeName)
    {
        return $"builder.AddScoped<{typeName}>();";
    }
    
    public string GenerateScopedInstance(string typeName, string interfaceTypeName)
    {
        return $"builder.AddScoped<{interfaceTypeName}, {typeName}>();";
    }

    public string GenerateTransientInstance(string typeName)
    {
        return $"builder.AddTransient<{typeName}>();";
    }
    
    public string GenerateTransientInstance(string typeName, string interfaceTypeName)
    {
        return $"builder.AddTransient<{interfaceTypeName}, {typeName}>();";
    }
    
    private string GenerateReuseAsSingleton(string typeName, string interfaceTypeName)
    {
        return $"Tools.ContainerRegistration.Microsoft.Extensions.ServiceCollectionExtension.ReUseSingleton<{typeName}, {interfaceTypeName}>(builder);";
    }

    private string GenerateAutoActivate(ServiceRegistrationEntity serviceRegistrationEntity)
    {
        if (!serviceRegistrationEntity.AutoActivate) return null;
        
        var type = string.Empty;

        if (serviceRegistrationEntity.RegisterAsSelf || serviceRegistrationEntity.FactoryRegistration != null)
            type = serviceRegistrationEntity.NamedTypeSymbol.Name;
        else
            type = serviceRegistrationEntity.RegisterAsInterfaces.FirstOrDefault();
        
        return $"provider.GetRequiredService<{type}>();";
    }

    public GeneratedServiceRegistrationEntity Generate(ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var registrationLine = new StringBuilder();
        var typeName =
            serviceRegistrationEntity.NamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var registeredAsSingletonYet = false;
        var registerAsSingleton = serviceRegistrationEntity.Scope == Scope.Singleton;
        
        if (serviceRegistrationEntity.FactoryRegistration != null)
        {
            registeredAsSingletonYet = registerAsSingleton;
            
            registrationLine.AppendLine(
                GenerateFactoryRegistration(
                    serviceRegistrationEntity.FactoryRegistration.FactoryMethodName,
                    typeName,
                    serviceRegistrationEntity.Scope));
        }
        else
        {
            // if (registerAsSingleton)
            // {
            //     registeredAsSingletonYet = registerAsSingleton;
            //     registrationLine.AppendLine(GenerateSingleInstance(typeName));
            // }
            // else if (registerAsScoped)
            // {
            //     registrationLine.AppendLine(GenerateScopedInstance(typeName));
            // }
        }

        foreach (var registerAsInterface in serviceRegistrationEntity.RegisterAsInterfaces)
        {
            if (registerAsInterface == serviceRegistrationEntity.NamedTypeSymbol.Name) continue;

            if (serviceRegistrationEntity.Scope == Scope.Singleton)
            {
                registrationLine.AppendLine(GenerateReuseAsSingleton(typeName, registerAsInterface));
            }
            else if (serviceRegistrationEntity.Scope == Scope.Scoped)
            {
                registrationLine.AppendLine(GenerateScopedInstance(typeName, registerAsInterface));
            }
            else
            {
                registrationLine.AppendLine(GenerateTransientInstance(typeName, registerAsInterface));
            }
        }    
        
        if (serviceRegistrationEntity.RegisterAsSelf && !registeredAsSingletonYet)
        {
            registrationLine.AppendLine(GenerateSelf(typeName, serviceRegistrationEntity.Scope));
        }

        var toReturn = new MicrosoftGeneratedServiceRegistrationEntity
        {
            Entity = registrationLine.ToString().Trim(),
            AutoActivate = GenerateAutoActivate(serviceRegistrationEntity)?.Trim()
        };
        return toReturn;
    }
}

public class MicrosoftGeneratedServiceRegistrationEntity : GeneratedServiceRegistrationEntity
{
    public string AutoActivate { get; set; }
}
