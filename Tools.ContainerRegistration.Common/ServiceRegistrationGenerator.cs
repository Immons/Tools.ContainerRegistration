using System.Diagnostics;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Common;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class ServiceRegistrationGenerator : ISourceGenerator
{
    protected abstract IGenerator Generator { get; }

    public void Initialize(GeneratorInitializationContext context) 
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        GlobalSettings.LoadSettings(context);
        
        var typesToRegister = DiscoverTypesForRegistration(context.Compilation).ToList();

        var source = GenerateServiceRegistrationCode(context.Compilation.AssemblyName, typesToRegister);

        context.AddSource($"{Generator.Name}_GeneratedServiceRegistration.g.cs", source);
    }
    
    private IEnumerable<INamedTypeSymbol> DiscoverTypesForRegistration(Compilation compilation)
    {
        var types = new List<INamedTypeSymbol>();
        
        bool MatchesPattern(string name, string pattern)
        {
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(name, regexPattern);
        }

        bool ShouldRegisterType(INamedTypeSymbol symbol)
        {
            if (symbol.IsStatic) return false;
            
            var hasManualRegistration = symbol.GetAttributes().Any(
                a => a.AttributeClass.Name == GlobalSettings.ManualRegistrationAttribute);
            if (hasManualRegistration) return false;
            
            var shouldRegisterBasedOnConventionName =
                GlobalSettings.RegisterTypesMatching.Any(pattern => MatchesPattern(symbol.ToDisplayString(), pattern));

            // Check if the type name matches any of the "ExcludedFromRegisteringMatching" patterns
            var isExcludedBasedOnConvention =
                GlobalSettings.ExcludedFromRegisteringMatching.Any(pattern => MatchesPattern(symbol.ToDisplayString(), pattern));

            var shouldRegisterBasedOnAttributes = symbol.GetAttributes().Any(
                a => a.AttributeClass.Name == GlobalSettings.SingletonAttribute ||
                     a.AttributeClass.Name == GlobalSettings.ScopedAttribute ||
                     a.AttributeClass.Name == GlobalSettings.ServiceRegistrationAttribute);
            
            return shouldRegisterBasedOnAttributes || (shouldRegisterBasedOnConventionName && !isExcludedBasedOnConvention);
        }

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            var interfaceDeclarations = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToList();

            foreach (var classDeclaration in classDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                if (symbol == null)
                    continue;

                if (ShouldRegisterType(symbol))
                    types.Add(symbol);
            }
            
            foreach (var interfaceDeclaration in interfaceDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration) as INamedTypeSymbol;
                if (symbol == null)
                    continue;

                if (ShouldRegisterType(symbol))
                    types.Add(symbol);
            }
        }
        return types;
    }

    private string GenerateServiceRegistrationCode(
        string assemblyName,
        List<INamedTypeSymbol> typesToRegister)
    {
        var requiredNamespaces = CollectRequiredNamespaces(typesToRegister);
        
        var serviceRegistration = Generator.GetServiceRegistration();
        serviceRegistration.Usings.Add(Generator.Namespace);
        serviceRegistration.Usings.AddRange(requiredNamespaces);
        serviceRegistration.Namespace = assemblyName;
        serviceRegistration.ContainerType = Generator.ContainerType;
        serviceRegistration.ContainerName = Generator.Name;
        serviceRegistration.ProviderType = Generator.ProviderType;

        foreach (var type in typesToRegister)
        {
            var serviceRegistrationEntity = new ServiceRegistrationEntity(type);
            
            GenerateForFactory(
                type,
                serviceRegistrationEntity);

            if (serviceRegistrationEntity.FactoryRegistration == null && CheckIfAbstract(type)) continue;
            if (CheckIfManual(type)) continue;
            
            GenerateForSelf(type, serviceRegistrationEntity);
            GenerateForServiceRegistration(type, serviceRegistrationEntity);
            GenerateForScoped(type, serviceRegistrationEntity);
            GenerateForSingleton(type, serviceRegistrationEntity);

            serviceRegistration.Entities.Add(serviceRegistrationEntity);
        }

        var code = serviceRegistration.Build(Generator);
        return code;
    }

    private void GenerateForSingleton(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var singleInstanceAttribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == GlobalSettings.SingletonAttribute);
        if (singleInstanceAttribute != null)
        {
            serviceRegistrationEntity.Scope = Scope.Singleton;
            if (singleInstanceAttribute.ConstructorArguments.Any(arg => (bool)arg.Value == true))
            {
                serviceRegistrationEntity.AutoActivate = true;
            }
        }
    }
    
    private void GenerateForScoped(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var scopedAttribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == GlobalSettings.ScopedAttribute);
        if (scopedAttribute != null)
        {
            serviceRegistrationEntity.Scope = Scope.Scoped;
        }
    }

    private bool CheckIfAbstract(INamedTypeSymbol type)
    {
        return type.IsAbstract;
    }

    private bool CheckIfManual(INamedTypeSymbol type)
    {
        return type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == GlobalSettings.ManualRegistrationAttribute) != null;
    }

    private void GenerateForServiceRegistration(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var serviceRegistrationAttribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == GlobalSettings.ServiceRegistrationAttribute);
        if (serviceRegistrationAttribute != null)
        {
            var interfaceTypes = serviceRegistrationAttribute.ConstructorArguments[0].Values;

            foreach (var interfaceType in interfaceTypes)
            {
                var interfaceTypeName = interfaceType.Value.ToString();
                serviceRegistrationEntity.RegisterAsInterfaces.Add(interfaceTypeName);
            }
        }
        else
        {
            if (GlobalSettings.RegisterAsAllInheritedTypes)
            {
                // Register all inherited interfaces, but only those from the same assembly
                serviceRegistrationEntity.RegisterAsInterfaces.AddRange(
                    type.AllInterfaces
                        .Where(SymbolBelongsToAllowedAssemblies)
                        .Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                );
            }
            else if (GlobalSettings.RegisterAsDirectlyInheritedTypes)
            {
                // Register only directly implemented interfaces, but only those from the same assembly
                serviceRegistrationEntity.RegisterAsInterfaces.AddRange(
                    type.Interfaces
                        .Where(SymbolBelongsToAllowedAssemblies)
                        .Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                );
            }

            bool SymbolBelongsToAllowedAssemblies(INamedTypeSymbol symbol)
            {
                if (!GlobalSettings.RegisterInterfacesOnlyFromThatAssemblies.Any()) return true;
                var assemblyName = symbol.ContainingAssembly.Name;
                return GlobalSettings.RegisterInterfacesOnlyFromThatAssemblies.Contains(assemblyName);
            }
        }
    }

    private void GenerateForSelf(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity)
    {
        if (GlobalSettings.RegisterAsSelf)
            serviceRegistrationEntity.RegisterAsSelf = true;
    }

    private void GenerateForFactory(INamedTypeSymbol type, ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var factoryAttribute = type.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass.Name == GlobalSettings.FactoryRegistrationAttribute);

        if (factoryAttribute != null)
        {
            var factoryMethodName = factoryAttribute.ConstructorArguments[0].Value.ToString();
            if (!string.IsNullOrWhiteSpace(factoryMethodName))
            {
                serviceRegistrationEntity.FactoryRegistration = new FactoryRegistrationEntity(factoryMethodName);
                return;
            }
        }

        return;
    }

    private HashSet<string> CollectRequiredNamespaces(IEnumerable<INamedTypeSymbol> types)
    {
        var namespaces = new HashSet<string>();

        foreach (var type in types)
        {
            // Collect the namespace of the class
            if (!string.IsNullOrWhiteSpace(type.ContainingNamespace?.ToString()))
            {
                namespaces.Add(type.ContainingNamespace.ToString());
            }

            // Collect the namespaces of the interfaces in the attribute
            var attribute = type.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass.Name == GlobalSettings.ServiceRegistrationAttribute);

            if (attribute != null)
            {
                var interfaceTypes = attribute.ConstructorArguments[0].Values;

                foreach (var interfaceType in interfaceTypes)
                {
                    var namedType = interfaceType.Value as INamedTypeSymbol;
                    if (namedType != null && !string.IsNullOrWhiteSpace(namedType.ContainingNamespace?.ToString()))
                    {
                        namespaces.Add(namedType.ContainingNamespace.ToString());
                    }
                }
            }
        }

        return namespaces;
    }
}