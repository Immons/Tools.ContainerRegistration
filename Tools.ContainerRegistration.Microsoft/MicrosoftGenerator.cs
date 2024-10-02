using System.Text;
using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Microsoft;

public class MicrosoftGenerator : IGenerator
{
    public string GenerateFactoryRegistration(string factoryMethodName, string interfaceTypeName)
    {
        return
            $"builder.AddSingleton(typeof({interfaceTypeName}), provider => {factoryMethodName}(typeof({interfaceTypeName}), provider));";
    }

    public string GenerateSelf(string typeName)
    {
        return $"builder.AddSingleton<{typeName}>();";
    }
    
    public string GenerateAs(string interfaceTypeName)
    {
        return $"builder.AddSingleton<{interfaceTypeName}>();";
    }

    public string GenerateSingleInstance(string typeName)
    {
        return $"builder.AddSingleton<{typeName}>();";
    }

    public string GenerateScopedInstance(string typeName)
    {
        return $"builder.AddScoped<{typeName}>();";
    }

    public string GenerateTransientInstance(string typeName)
    {
        return $"builder.AddTransient<{typeName}>();";
    }

    public string GetContainerType()
    {
        return $"IServiceCollection";
    }

    public string GetNamespace()
    {
        return "Microsoft.Extensions.DependencyInjection";
    }

    public string Generate(ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var registrationLine = new StringBuilder();
        var typeName =
            serviceRegistrationEntity.NamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        if (serviceRegistrationEntity.FactoryRegistration != null)
        {
            registrationLine.AppendLine(
                GenerateFactoryRegistration(
                    serviceRegistrationEntity.FactoryRegistration.FactoryMethodName,
                    typeName));
        }
        else
        {
            if (serviceRegistrationEntity.SingleInstance)
            {
                registrationLine.AppendLine(GenerateSingleInstance(typeName));
            }
            else
            {
                registrationLine.AppendLine(GenerateTransientInstance(typeName));
            }
        }

        foreach (var registerAsInterface in serviceRegistrationEntity.RegisterAsInterfaces)
        {
            if (registerAsInterface == serviceRegistrationEntity.NamedTypeSymbol.Name) continue;

            if (serviceRegistrationEntity.SingleInstance)
            {
                var line =
                    $"Tools.ContainerRegistration.Microsoft.Extensions.ServiceCollectionExtension.ReUseSingleton<{typeName}, {registerAsInterface}>(builder);";
                registrationLine.AppendLine(line);
            }
            else
            {
                registrationLine.AppendLine($"builder.AddTransient<{registerAsInterface}, {typeName}>();");
            }
        }
        
        if (serviceRegistrationEntity.RegisterAsSelf)
        {
            registrationLine.AppendLine(GenerateSelf(typeName));
        }

        return registrationLine.ToString();
    }

    public string Name => "ServiceCollection";
}
