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

                if (GlobalSettings.RegisterTypesEndingWith.Any(name => symbol.Name.EndsWith(name) ||
                                                                       symbol.GetAttributes().Any(a => a.AttributeClass.Name == GlobalSettings.ServiceRegistrationAttribute)) &&
                    !symbol.GetAttributes().Any(a => GlobalSettings.AttributesToExclude.Contains(a.AttributeClass.Name)))
                {
                    types.Add(symbol);
                }
            }
            
            foreach (var interfaceDeclaration in interfaceDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration) as INamedTypeSymbol;
                if (symbol == null)
                    continue;

                if ((GlobalSettings.RegisterTypesEndingWith.Any(name => symbol.Name.EndsWith(name)||
                                                                        symbol.GetAttributes().Any(a => a.AttributeClass.Name == GlobalSettings.ServiceRegistrationAttribute))) &&
                    !symbol.GetAttributes().Any(a => GlobalSettings.AttributesToExclude.Contains(a.AttributeClass.Name)))
                {
                    types.Add(symbol);
                }
            }
        }
        return types;
    }

    private string GenerateServiceRegistrationCode(
        string assemblyName,
        List<INamedTypeSymbol> typesToRegister)
    {
        var requiredNamespaces = CollectRequiredNamespaces(typesToRegister);

        var sb = new StringBuilder();

        foreach (var ns in requiredNamespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        
        sb.AppendLine($"using {Generator.GetNamespace()};");
        sb.AppendLine();
        sb.AppendLine($"namespace {assemblyName};");
        sb.AppendLine();
        sb.AppendLine($"public static class {Generator.Name}_GeneratedServiceRegistration");
        sb.AppendLine("{");
        sb.AppendLine($"    public static void RegisterServices({Generator.GetContainerType()} builder)");
        sb.AppendLine("    {");

        foreach (var type in typesToRegister)
        {
            var serviceRegistrationEntity = new ServiceRegistrationEntity(type);
            
            GenerateForFactory(
                type,
                serviceRegistrationEntity);

            if (serviceRegistrationEntity.FactoryRegistration == null && CheckIfAbstract(type)) continue;
            if (CheckIfManual(type)) continue;
            
            // var registerLine = Generator.GenerateRegisterType(type.Name);
            GenerateForSelf(type, serviceRegistrationEntity);
            GenerateForServiceRegistration(type, serviceRegistrationEntity);
            GenerateForSingleton(type, serviceRegistrationEntity);

            var registerLine = Generator.Generate(serviceRegistrationEntity);
            sb.AppendLine($"        {registerLine}");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private void GenerateForSingleton(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var singleInstanceAttribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == GlobalSettings.SingletonAttribute);
        if (singleInstanceAttribute != null)
        {
            serviceRegistrationEntity.SingleInstance = true;
            if (singleInstanceAttribute.ConstructorArguments.Any(arg => (bool)arg.Value == true))
            {
                serviceRegistrationEntity.AutoActivate = true;
            }
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
            if (!GlobalSettings.ExcludedFromRegisteringAsConventionInterface.Any(suffix => type.Name.EndsWith(suffix)))
                serviceRegistrationEntity.RegisterAsInterfaces.Add(type.IsAbstract ? type.Name : GlobalSettings.GetInterfaceNamingConvention(type.Name));
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