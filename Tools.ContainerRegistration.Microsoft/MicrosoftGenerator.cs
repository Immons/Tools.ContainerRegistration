using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;
using Tools.ContainerRegistration.Microsoft.Models;

namespace Tools.ContainerRegistration.Microsoft;

public class MicrosoftGenerator : IGenerator
{
    public string Name => "ServiceCollection";
    public string ProviderType => "IServiceProvider";
    public string ContainerType => "IServiceCollection";
    public string Namespace => "Microsoft.Extensions.DependencyInjection";

    public ServiceRegistration GetServiceRegistration() => new MicrosoftServiceRegistration();

    /// <summary>
    /// Checks if a type symbol represents an open generic type (has unbound type parameters).
    /// </summary>
    private static bool IsOpenGenericType(INamedTypeSymbol type)
    {
        return type.IsGenericType && type.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);
    }

    /// <summary>
    /// Gets the type name formatted for registration code generation.
    /// For open generics, returns typeof(Namespace.Type&lt;,&gt;) format.
    /// For closed generics and non-generics, returns the fully qualified name for use in generic method syntax.
    /// </summary>
    private static string GetTypeNameForRegistration(INamedTypeSymbol type)
    {
        if (!type.IsGenericType)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        var allTypeArgumentsAreTypeParameters = type.TypeArguments.All(t => t.TypeKind == TypeKind.TypeParameter);

        if (allTypeArgumentsAreTypeParameters)
        {
            // This is an open generic - convert to unbound format
            var unboundType = type.ConstructUnboundGenericType();
            return unboundType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private string GenerateFactoryRegistration(string factoryMethodName, string interfaceTypeName, Scope scope)
    {
        if (scope == Scope.Singleton)
            return $"builder.AddSingleton(typeof({interfaceTypeName}), provider => {factoryMethodName}(typeof({interfaceTypeName}), provider));";
        if (scope == Scope.Scoped)
            return $"builder.AddScoped(typeof({interfaceTypeName}), provider => {factoryMethodName}(typeof({interfaceTypeName}), provider));";

        return $"builder.AddTransient(typeof({interfaceTypeName}), provider => {factoryMethodName}(typeof({interfaceTypeName}), provider));";
    }

    private string GenerateSelfWithInjection(string typeName, Scope scope, List<InjectorDependency> injectorDependencies)
    {
        var injectionCode = GenerateInjectionCode("instance", injectorDependencies);
        var scopeMethod = scope switch
        {
            Scope.Singleton => "AddSingleton",
            Scope.Scoped => "AddScoped",
            _ => "AddTransient"
        };

        return $"builder.{scopeMethod}<{typeName}>(provider => {{ var instance = ActivatorUtilities.CreateInstance<{typeName}>(provider); {injectionCode} return instance; }});";
    }

    private string GenerateAsWithInjection(string typeName, string interfaceTypeName, Scope scope, List<InjectorDependency> injectorDependencies)
    {
        var injectionCode = GenerateInjectionCode("instance", injectorDependencies);
        var scopeMethod = scope switch
        {
            Scope.Singleton => "AddSingleton",
            Scope.Scoped => "AddScoped",
            _ => "AddTransient"
        };

        return $"builder.{scopeMethod}<{interfaceTypeName}>(provider => {{ var instance = ActivatorUtilities.CreateInstance<{typeName}>(provider); {injectionCode} return instance; }});";
    }

    private string GenerateInjectionCode(string instanceName, List<InjectorDependency> injectorDependencies)
    {
        var sb = new StringBuilder();
        foreach (var dep in injectorDependencies)
        {
            sb.Append($"(({dep.InjectorInterfaceFullName}){instanceName}).Inject(provider.GetRequiredService<{dep.DependencyTypeFullName}>()); ");
        }
        return sb.ToString();
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

    // typeof() based registrations for open generic types
    public string GenerateSingleInstanceTypeOf(string typeName, string interfaceTypeName)
    {
        return $"builder.AddSingleton(typeof({interfaceTypeName}), typeof({typeName}));";
    }

    public string GenerateScopedInstanceTypeOf(string typeName, string interfaceTypeName)
    {
        return $"builder.AddScoped(typeof({interfaceTypeName}), typeof({typeName}));";
    }

    public string GenerateTransientInstanceTypeOf(string typeName, string interfaceTypeName)
    {
        return $"builder.AddTransient(typeof({interfaceTypeName}), typeof({typeName}));";
    }

    private string GenerateReuseAsSingleton(string typeName, string interfaceTypeName)
    {
        return $"global::Tools.ContainerRegistration.Microsoft.Extensions.ServiceCollectionExtension.ReUseSingleton<{typeName}, {interfaceTypeName}>(builder);";
    }

    private string GenerateForwardingRegistration(string typeName, string interfaceTypeName, Scope scope)
    {
        var scopeMethod = scope switch
        {
            Scope.Singleton => "AddSingleton",
            Scope.Scoped => "AddScoped",
            _ => "AddTransient"
        };

        return $"builder.{scopeMethod}<{interfaceTypeName}>(provider => provider.GetRequiredService<{typeName}>());";
    }

    private string GenerateAutoActivate(ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var hasAutoActivate = serviceRegistrationEntity.AutoActivate;
        var hasOnActivatedCallbacks = serviceRegistrationEntity.OnActivatedCallbacks.Any();

        if (!hasAutoActivate && !hasOnActivatedCallbacks) return null;

        var typeName = serviceRegistrationEntity.NamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Determine the type to resolve - prefer self, then first interface
        var resolveType = string.Empty;
        if (serviceRegistrationEntity.RegisterAsSelf || serviceRegistrationEntity.FactoryRegistration != null)
            resolveType = typeName;
        else
            resolveType = serviceRegistrationEntity.RegisterAsInterfaces.FirstOrDefault() ?? typeName;

        var sb = new StringBuilder();

        if (hasOnActivatedCallbacks)
        {
            // Generate: var instance = provider.GetRequiredService<Type>();
            sb.AppendLine($"var instance_{serviceRegistrationEntity.NamedTypeSymbol.Name} = provider.GetRequiredService<{resolveType}>();");

            foreach (var callback in serviceRegistrationEntity.OnActivatedCallbacks)
            {
                if (callback.IsInstanceMethod)
                {
                    // Instance method: instance.MethodName();
                    sb.AppendLine($"instance_{serviceRegistrationEntity.NamedTypeSymbol.Name}.{callback.MethodName}();");
                }
                else
                {
                    // Static method: TargetType.MethodName(instance);
                    sb.AppendLine($"{callback.TargetTypeFullName}.{callback.MethodName}(instance_{serviceRegistrationEntity.NamedTypeSymbol.Name});");
                }
            }
        }
        else if (hasAutoActivate)
        {
            // Just auto-activate without callbacks
            sb.AppendLine($"provider.GetRequiredService<{resolveType}>();");
        }

        return sb.ToString();
    }

    public GeneratedServiceRegistrationEntity Generate(ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var registrationLine = new StringBuilder();
        var namedTypeSymbol = serviceRegistrationEntity.NamedTypeSymbol;
        var isOpenGeneric = IsOpenGenericType(namedTypeSymbol);

        // For open generics, use the unbound format (e.g., Type<,>)
        // For closed generics and non-generics, use the full type name
        var typeName = isOpenGeneric
            ? GetTypeNameForRegistration(namedTypeSymbol)
            : namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var shortTypeName = namedTypeSymbol.Name;

        var registeredAsSingletonYet = false;
        var registerAsSingleton = serviceRegistrationEntity.Scope == Scope.Singleton;
        var hasInjectorDependencies = serviceRegistrationEntity.InjectorDependencies.Any();

        if (serviceRegistrationEntity.FactoryRegistration != null)
        {
            registeredAsSingletonYet = registerAsSingleton;

            // Register the concrete type with factory
            registrationLine.AppendLine(
                GenerateFactoryRegistration(
                    serviceRegistrationEntity.FactoryRegistration.FactoryMethodName,
                    typeName,
                    serviceRegistrationEntity.Scope));

            // Also register interfaces as forwarding registrations
            foreach (var registerAsInterface in serviceRegistrationEntity.RegisterAsInterfaces)
            {
                if (registerAsInterface == shortTypeName || registerAsInterface == typeName) continue;

                registrationLine.AppendLine(
                    GenerateForwardingRegistration(typeName, registerAsInterface, serviceRegistrationEntity.Scope));
            }
        }

        // Handle types with IInjector dependencies using factory pattern
        if (hasInjectorDependencies && serviceRegistrationEntity.FactoryRegistration == null)
        {
            foreach (var registerAsInterface in serviceRegistrationEntity.RegisterAsInterfaces)
            {
                if (registerAsInterface == shortTypeName || registerAsInterface == typeName) continue;

                registrationLine.AppendLine(GenerateAsWithInjection(
                    typeName,
                    registerAsInterface,
                    serviceRegistrationEntity.Scope,
                    serviceRegistrationEntity.InjectorDependencies));
            }

            if (serviceRegistrationEntity.RegisterAsSelf)
            {
                registrationLine.AppendLine(GenerateSelfWithInjection(
                    typeName,
                    serviceRegistrationEntity.Scope,
                    serviceRegistrationEntity.InjectorDependencies));
            }
        }
        else if (serviceRegistrationEntity.FactoryRegistration == null)
        {
            // Standard registration without IInjector
            foreach (var registerAsInterface in serviceRegistrationEntity.RegisterAsInterfaces)
            {
                if (registerAsInterface == shortTypeName ||
                    registerAsInterface == typeName) continue;

                // For open generics, use typeof() based registration
                if (isOpenGeneric)
                {
                    if (serviceRegistrationEntity.Scope == Scope.Singleton)
                    {
                        registrationLine.AppendLine(GenerateSingleInstanceTypeOf(typeName, registerAsInterface));
                    }
                    else if (serviceRegistrationEntity.Scope == Scope.Scoped)
                    {
                        registrationLine.AppendLine(GenerateScopedInstanceTypeOf(typeName, registerAsInterface));
                    }
                    else
                    {
                        registrationLine.AppendLine(GenerateTransientInstanceTypeOf(typeName, registerAsInterface));
                    }
                }
                else
                {
                    // Standard generic method syntax for closed/non-generic types
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
            }

            if (serviceRegistrationEntity.RegisterAsSelf && !registeredAsSingletonYet)
            {
                registrationLine.AppendLine(GenerateSelf(typeName, serviceRegistrationEntity.Scope));
            }
        }

        var toReturn = new MicrosoftGeneratedServiceRegistrationEntity
        {
            Entity = registrationLine.ToString().Trim(),
            AutoActivate = GenerateAutoActivate(serviceRegistrationEntity)?.Trim()
        };
        return toReturn;
    }
}