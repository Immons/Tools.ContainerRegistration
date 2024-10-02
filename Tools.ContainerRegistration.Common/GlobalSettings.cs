using System;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Common.Generators;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Common;

public static class GlobalSettings
{
    public const string SingletonAttribute = nameof(SingletonAttribute);
    public const string ScopedAttribute = nameof(ScopedAttribute);
    public const string ManualRegistrationAttribute = nameof(ManualRegistrationAttribute);
    public const string ServiceRegistrationAttribute = nameof(ServiceRegistrationAttribute);
    public const string FactoryRegistrationAttribute = nameof(FactoryRegistrationAttribute);
    
    public static string GetInterfaceNamingConvention(string typeName) => $"{InterfaceNamingConvention}{typeName}";
    public static string[] ExcludedFromRegisteringAsConventionInterface { get; private set; } = new[] { "Page" };
    public static string[] ExcludedFromRegisteringTypesEndingWith { get; private set; } = Array.Empty<string>();
    
    public static string[] RegisterTypesEndingWith { get; private set; } = new[] { "Service", "Map", "Factory", "Repository", "Action", "CommandBuilder", "Page", "ViewModel"};
    public static string InterfaceNamingConvention { get; private set; } = "I";
    public static bool RegisterAsSelf { get; private set; } = true;
    
    public static void LoadSettings(GeneratorExecutionContext context)
    {
        var additionalFile = context.AdditionalFiles.FirstOrDefault(file => file.Path.EndsWith("ioc_config.json"));
        if (additionalFile != null)
        {
            var text = additionalFile.GetText(context.CancellationToken)?.ToString();
            var config = JsonSerializer.Deserialize<IocConfig>(text);
            ExcludedFromRegisteringAsConventionInterface = config?.ExcludedFromRegisteringAsConventionInterface ?? ExcludedFromRegisteringAsConventionInterface;
            RegisterTypesEndingWith = config?.RegisterTypesEndingWith ?? RegisterTypesEndingWith;
            InterfaceNamingConvention = config?.InterfaceNamingConvention ?? InterfaceNamingConvention;
            ExcludedFromRegisteringTypesEndingWith = config?.ExcludedFromRegisteringTypesEndingWith ?? ExcludedFromRegisteringTypesEndingWith;
            RegisterAsSelf = config?.RegisterAsSelf ?? RegisterAsSelf;
        }
    }
}