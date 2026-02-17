using System.Collections.Immutable;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Common;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

// Diagnostic descriptors for logging
public static class GeneratorDiagnostics
{
    public static readonly DiagnosticDescriptor InfoLog = new(
        id: "TCR001",
        title: "Generator Info",
        messageFormat: "{0}",
        category: "Tools.ContainerRegistration",
        DiagnosticSeverity.Warning, // Warning żeby było widoczne, Info często jest ukryte
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ErrorLog = new(
        id: "TCR002",
        title: "Generator Error",
        messageFormat: "{0}",
        category: "Tools.ContainerRegistration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

public abstract class ServiceRegistrationGenerator : IIncrementalGenerator
{
    protected abstract IGenerator Generator { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create provider for type declarations (classes and interfaces)
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax or InterfaceDeclarationSyntax,
                transform: static (ctx, _) => GetTypeDeclarationForGeneration(ctx))
            .Where(static t => t is not null)
            .Select(static (t, _) => t!);

        // Combine with compilation and additional files
        var compilationAndTypes = context.CompilationProvider
            .Combine(typeDeclarations.Collect())
            .Combine(context.AdditionalTextsProvider.Collect());

        // Register source output
        context.RegisterSourceOutput(compilationAndTypes, (spc, source) =>
        {
            var ((compilation, types), additionalFiles) = source;
            Execute(compilation, types, additionalFiles, spc);
        });
    }

    private static TypeDeclarationSyntax? GetTypeDeclarationForGeneration(GeneratorSyntaxContext context)
    {
        return context.Node as TypeDeclarationSyntax;
    }

    private void Log(SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.InfoLog, Location.None, message));
    }

    private void LogError(SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.ErrorLog, Location.None, message));
    }

    private void Execute(
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> typeDeclarations,
        ImmutableArray<AdditionalText> additionalFiles,
        SourceProductionContext context)
    {
        Log(context, $"[{Generator.Name}] Starting for {compilation.AssemblyName}, types: {typeDeclarations.Length}, files: {additionalFiles.Length}");

        try
        {
            ExecuteInternal(compilation, typeDeclarations, additionalFiles, context);
            Log(context, $"[{Generator.Name}] Completed successfully for {compilation.AssemblyName}");
        }
        catch (System.Exception ex)
        {
            LogError(context, $"[{Generator.Name}] Exception: {ex.GetType().Name}: {ex.Message}");

            // Always emit diagnostic file on error
            var diagnosticContent = $@"// Generator Error Diagnostic
// Generator: {Generator.Name}
// Assembly: {compilation.AssemblyName}
// TypeDeclarations count: {typeDeclarations.Length}
// AdditionalFiles count: {additionalFiles.Length}
// AdditionalFiles: {string.Join(", ", additionalFiles.Select(f => System.IO.Path.GetFileName(f.Path)))}
// Exception Type: {ex.GetType().FullName}
// Exception Message: {ex.Message}
// Stack Trace:
/*
{ex.StackTrace}
*/
";
            context.AddSource($"{Generator.Name}_GeneratorError.g.cs", diagnosticContent);
        }
    }

    private void ExecuteInternal(
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> typeDeclarations,
        ImmutableArray<AdditionalText> additionalFiles,
        SourceProductionContext context)
    {
        if (typeDeclarations.IsDefaultOrEmpty)
            return;

        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        var stepSw = System.Diagnostics.Stopwatch.StartNew();

        var settings = GlobalSettings.LoadSettings(additionalFiles, context.CancellationToken);
        var loadSettingsMs = stepSw.ElapsedMilliseconds;
        stepSw.Restart();

        var typesToRegister = DiscoverTypesForRegistration(compilation, typeDeclarations, settings, context.CancellationToken).ToList();
        var discoverMs = stepSw.ElapsedMilliseconds;
        stepSw.Restart();

        if (settings.SplitGeneratedFiles)
        {
            var sourceFiles = GenerateSplitServiceRegistrationCode(compilation.AssemblyName, typesToRegister, settings);
            foreach (var sourceFile in sourceFiles)
            {
                context.AddSource(sourceFile.HintName, sourceFile.Content);
            }
        }
        else
        {
            var source = GenerateServiceRegistrationCode(compilation.AssemblyName, typesToRegister, settings);
            context.AddSource($"{Generator.Name}_GeneratedServiceRegistration.g.cs", source);
        }
        var generateMs = stepSw.ElapsedMilliseconds;
        totalSw.Stop();

        Log(context, $"[{Generator.Name}] Timing for {compilation.AssemblyName}: LoadSettings={loadSettingsMs}ms, DiscoverTypes={discoverMs}ms ({typesToRegister.Count} types), GenerateCode={generateMs}ms, Total={totalSw.ElapsedMilliseconds}ms");
    }

    private IEnumerable<INamedTypeSymbol> DiscoverTypesForRegistration(
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> typeDeclarations,
        GlobalSettings settings,
        CancellationToken cancellationToken)
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
                a => a.AttributeClass?.Name == GlobalSettings.ManualRegistrationAttribute);
            if (hasManualRegistration) return false;

            var shouldRegisterBasedOnConventionName =
                settings.RegisterTypesMatching.Any(pattern => MatchesPattern(symbol.ToDisplayString(), pattern));

            var isExcludedBasedOnConvention =
                settings.ExcludedFromRegisteringMatching.Any(pattern => MatchesPattern(symbol.ToDisplayString(), pattern));

            var shouldRegisterBasedOnAttributes = symbol.GetAttributes().Any(
                a => a.AttributeClass?.Name == GlobalSettings.SingletonAttribute ||
                     a.AttributeClass?.Name == GlobalSettings.ScopedAttribute ||
                     a.AttributeClass?.Name == GlobalSettings.ServiceRegistrationAttribute);

            return shouldRegisterBasedOnAttributes || (shouldRegisterBasedOnConventionName && !isExcludedBasedOnConvention);
        }

        // Process type declarations from syntax
        foreach (var typeDeclaration in typeDeclarations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) as INamedTypeSymbol;

            if (symbol == null)
                continue;

            if (ShouldRegisterType(symbol))
                types.Add(symbol);
        }

        // Scan types from referenced assemblies specified in ScanAssemblies config
        if (settings.ScanAssemblies.Any())
        {
            foreach (var reference in compilation.References)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assemblySymbol == null)
                    continue;

                if (!settings.ScanAssemblies.Contains(assemblySymbol.Name))
                    continue;

                var typesFromAssembly = GetAllTypesFromNamespace(assemblySymbol.GlobalNamespace);
                foreach (var type in typesFromAssembly)
                {
                    if (ShouldRegisterType(type))
                        types.Add(type);
                }
            }
        }

        return types;
    }

    private IEnumerable<INamedTypeSymbol> GetAllTypesFromNamespace(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypesFromNamespace(nestedNamespace))
            {
                yield return type;
            }
        }
    }

    private string GenerateServiceRegistrationCode(
        string assemblyName,
        List<INamedTypeSymbol> typesToRegister,
        GlobalSettings settings)
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

            GenerateForFactory(type, serviceRegistrationEntity);

            if (serviceRegistrationEntity.FactoryRegistration == null && CheckIfAbstract(type)) continue;
            if (CheckIfManual(type)) continue;

            GenerateForSelf(type, serviceRegistrationEntity, settings);
            GenerateForServiceRegistration(type, serviceRegistrationEntity, settings);
            GenerateForScoped(type, serviceRegistrationEntity);
            GenerateForSingleton(type, serviceRegistrationEntity);
            GenerateForInjectorDependencies(type, serviceRegistrationEntity);
            GenerateForOnActivated(type, serviceRegistrationEntity);

            serviceRegistration.Entities.Add(serviceRegistrationEntity);
        }

        var code = serviceRegistration.Build(Generator);
        return code;
    }

    private IEnumerable<GeneratedSourceFile> GenerateSplitServiceRegistrationCode(
        string assemblyName,
        List<INamedTypeSymbol> typesToRegister,
        GlobalSettings settings)
    {
        var serviceRegistration = Generator.GetServiceRegistration();
        serviceRegistration.Usings.Add(Generator.Namespace);
        serviceRegistration.Namespace = assemblyName;
        serviceRegistration.ContainerType = Generator.ContainerType;
        serviceRegistration.ContainerName = Generator.Name;
        serviceRegistration.ProviderType = Generator.ProviderType;

        // Group types by their suffix pattern
        var groupedEntities = new Dictionary<string, List<ServiceRegistrationEntity>>();

        foreach (var type in typesToRegister)
        {
            var serviceRegistrationEntity = new ServiceRegistrationEntity(type);

            GenerateForFactory(type, serviceRegistrationEntity);

            if (serviceRegistrationEntity.FactoryRegistration == null && CheckIfAbstract(type)) continue;
            if (CheckIfManual(type)) continue;

            GenerateForSelf(type, serviceRegistrationEntity, settings);
            GenerateForServiceRegistration(type, serviceRegistrationEntity, settings);
            GenerateForScoped(type, serviceRegistrationEntity);
            GenerateForSingleton(type, serviceRegistrationEntity);
            GenerateForInjectorDependencies(type, serviceRegistrationEntity);
            GenerateForOnActivated(type, serviceRegistrationEntity);

            // Determine which group this type belongs to
            var groupName = settings.GetGroupForType(type.ToDisplayString());

            if (!groupedEntities.ContainsKey(groupName))
                groupedEntities[groupName] = new List<ServiceRegistrationEntity>();

            groupedEntities[groupName].Add(serviceRegistrationEntity);
        }

        return serviceRegistration.BuildSplit(Generator, groupedEntities);
    }

    private void GenerateForSingleton(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var singleInstanceAttribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == GlobalSettings.SingletonAttribute);
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
        var scopedAttribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == GlobalSettings.ScopedAttribute);
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
        return type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == GlobalSettings.ManualRegistrationAttribute) != null;
    }

    private void GenerateForServiceRegistration(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity,
        GlobalSettings settings)
    {
        var serviceRegistrationAttribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == GlobalSettings.ServiceRegistrationAttribute);
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
            if (settings.RegisterAsAllInheritedTypes)
            {
                serviceRegistrationEntity.RegisterAsInterfaces.AddRange(
                    type.AllInterfaces
                        .Where(SymbolBelongsToAllowedAssemblies)
                        .Select(GetTypeNameForRegistration)
                );
            }
            else if (settings.RegisterAsDirectlyInheritedTypes)
            {
                serviceRegistrationEntity.RegisterAsInterfaces.AddRange(
                    type.Interfaces
                        .Where(SymbolBelongsToAllowedAssemblies)
                        .Select(GetTypeNameForRegistration)
                );
            }

            bool SymbolBelongsToAllowedAssemblies(INamedTypeSymbol symbol)
            {
                if (!settings.RegisterInterfacesOnlyFromThatAssemblies.Any()) return true;
                var assemblyName = symbol.ContainingAssembly.Name;
                return settings.RegisterInterfacesOnlyFromThatAssemblies.Contains(assemblyName);
            }
        }
    }

    /// <summary>
    /// Gets the type name for code generation, handling open generics properly.
    /// For open generic types like IPipelineBehavior&lt;TRequest, TResponse&gt;,
    /// returns the unbound generic format: global::Namespace.Type&lt;,&gt;
    /// </summary>
    private static string GetTypeNameForRegistration(INamedTypeSymbol type)
    {
        if (!type.IsGenericType)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // Check if all type arguments are type parameters (unbound generic)
        var allTypeArgumentsAreTypeParameters = type.TypeArguments.All(t => t.TypeKind == TypeKind.TypeParameter);

        if (allTypeArgumentsAreTypeParameters)
        {
            // This is an open generic like IPipelineBehavior<TRequest, TResponse>
            // Convert to unbound generic format: global::Namespace.IPipelineBehavior<,>
            var unboundType = type.ConstructUnboundGenericType();
            return unboundType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // Closed generic with concrete type arguments
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private void GenerateForSelf(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity,
        GlobalSettings settings)
    {
        if (settings.RegisterAsSelf)
            serviceRegistrationEntity.RegisterAsSelf = true;
    }

    private void GenerateForInjectorDependencies(
        INamedTypeSymbol type,
        ServiceRegistrationEntity serviceRegistrationEntity)
    {
        // Check if type has [InjectDependencies] attribute
        var hasInjectDependenciesAttribute = type.GetAttributes()
            .Any(a => a.AttributeClass?.Name == GlobalSettings.InjectDependenciesAttribute);

        // Also check base types for the attribute (inherited)
        var baseType = type.BaseType;
        while (!hasInjectDependenciesAttribute && baseType != null)
        {
            hasInjectDependenciesAttribute = baseType.GetAttributes()
                .Any(a => a.AttributeClass?.Name == GlobalSettings.InjectDependenciesAttribute);
            baseType = baseType.BaseType;
        }

        if (!hasInjectDependenciesAttribute)
            return;

        // Find all IInjector<T> interfaces implemented by this type and its base types
        var allInterfaces = type.AllInterfaces;
        foreach (var iface in allInterfaces)
        {
            if (iface.IsGenericType &&
                iface.Name == GlobalSettings.IInjectorInterface &&
                iface.TypeArguments.Length == 1)
            {
                var typeArgument = iface.TypeArguments[0];
                var dependencyTypeFullName = typeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var injectorInterfaceFullName = iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Avoid duplicates
                if (!serviceRegistrationEntity.InjectorDependencies.Exists(d => d.DependencyTypeFullName == dependencyTypeFullName))
                {
                    serviceRegistrationEntity.InjectorDependencies.Add(new InjectorDependency
                    {
                        InjectorInterfaceFullName = injectorInterfaceFullName,
                        DependencyTypeFullName = dependencyTypeFullName
                    });
                }
            }
        }
    }

    private void GenerateForFactory(INamedTypeSymbol type, ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var factoryAttribute = type.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == GlobalSettings.FactoryRegistrationAttribute);

        if (factoryAttribute != null)
        {
            var factoryMethodName = factoryAttribute.ConstructorArguments[0].Value.ToString();
            if (!string.IsNullOrWhiteSpace(factoryMethodName))
            {
                // Check for scoped parameter (second constructor argument)
                var scoped = factoryAttribute.ConstructorArguments.Length > 1 &&
                             factoryAttribute.ConstructorArguments[1].Value is bool scopedValue &&
                             scopedValue;

                serviceRegistrationEntity.FactoryRegistration = new FactoryRegistrationEntity(factoryMethodName, scoped);

                // If factory is scoped, update the entity scope
                if (scoped)
                {
                    serviceRegistrationEntity.Scope = Scope.Scoped;
                }

                serviceRegistrationEntity.RegisterAsInterfaces.Add(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                return;
            }
        }
    }

    private void GenerateForOnActivated(INamedTypeSymbol type, ServiceRegistrationEntity serviceRegistrationEntity)
    {
        var onActivatedAttributes = type.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name == GlobalSettings.OnActivatedAttribute)
            .ToList();

        foreach (var attribute in onActivatedAttributes)
        {
            var constructorArgs = attribute.ConstructorArguments;

            if (constructorArgs.Length == 1 && constructorArgs[0].Value is string methodName)
            {
                // Instance method: [OnActivated("MethodName")]
                serviceRegistrationEntity.OnActivatedCallbacks.Add(new OnActivatedCallback
                {
                    MethodName = methodName,
                    IsInstanceMethod = true
                });
            }
            else if (constructorArgs.Length == 2)
            {
                // Static method: [OnActivated(typeof(TargetType), "MethodName")]
                var targetType = constructorArgs[0].Value as INamedTypeSymbol;
                var staticMethodName = constructorArgs[1].Value as string;

                if (targetType != null && !string.IsNullOrWhiteSpace(staticMethodName))
                {
                    serviceRegistrationEntity.OnActivatedCallbacks.Add(new OnActivatedCallback
                    {
                        TargetTypeFullName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        MethodName = staticMethodName,
                        IsInstanceMethod = false
                    });
                }
            }
        }
    }

    private HashSet<string> CollectRequiredNamespaces(IEnumerable<INamedTypeSymbol> types)
    {
        var namespaces = new HashSet<string>();

        foreach (var type in types)
        {
            if (!string.IsNullOrWhiteSpace(type.ContainingNamespace?.ToString()))
            {
                namespaces.Add(type.ContainingNamespace.ToString());
            }

            var attribute = type.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == GlobalSettings.ServiceRegistrationAttribute);

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
