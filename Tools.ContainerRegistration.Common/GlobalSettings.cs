using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Tools.ContainerRegistration.Common.Generators;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Common;

public class GlobalSettings
{
    public const string SingletonAttribute = nameof(SingletonAttribute);
    public const string ScopedAttribute = nameof(ScopedAttribute);
    public const string ManualRegistrationAttribute = nameof(ManualRegistrationAttribute);
    public const string ServiceRegistrationAttribute = nameof(ServiceRegistrationAttribute);
    public const string FactoryRegistrationAttribute = nameof(FactoryRegistrationAttribute);
    public const string InjectDependenciesAttribute = nameof(InjectDependenciesAttribute);
    public const string OnActivatedAttribute = nameof(OnActivatedAttribute);
    public const string IInjectorInterface = "IInjector";

    private static readonly string[] DefaultRegisterTypesMatching = new[]
    {
        "*Service*", "*Map*", "*Factory*", "*Repository*",
        "*Action*", "*CommandBuilder*", "*Page*", "*ViewModel*"
    };

    public string[] RegisterInterfacesOnlyFromThatAssemblies { get; private set; } = Array.Empty<string>();
    public string[] ScanAssemblies { get; private set; } = Array.Empty<string>();
    public string[] ExcludedFromRegisteringMatching { get; private set; } = Array.Empty<string>();
    public string[] RegisterTypesMatching { get; private set; } = DefaultRegisterTypesMatching;
    public bool RegisterAsSelf { get; private set; } = true;
    public bool RegisterAsAllInheritedTypes { get; private set; } = true;
    public bool RegisterAsDirectlyInheritedTypes { get; private set; } = true;
    public bool SplitGeneratedFiles { get; private set; } = false;

    public static GlobalSettings LoadSettings(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
    {
        var settings = new GlobalSettings();

        var additionalFile = additionalFiles.FirstOrDefault(file => file.Path.EndsWith("ioc_config.json"));
        if (additionalFile != null)
        {
            var text = additionalFile.GetText(cancellationToken)?.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                var config = JsonSerializer.Deserialize<IocConfig>(text);
                settings.RegisterTypesMatching = config?.RegisterTypesMatching ?? DefaultRegisterTypesMatching;
                settings.ExcludedFromRegisteringMatching = config?.ExcludedFromRegisteringMatching ?? Array.Empty<string>();
                settings.RegisterAsSelf = config?.RegisterAsSelf ?? true;
                settings.RegisterAsAllInheritedTypes = config?.RegisterAsAllInheritedTypes ?? true;
                settings.RegisterAsDirectlyInheritedTypes = config?.RegisterAsDirectlyInheritedTypes ?? true;
                settings.RegisterInterfacesOnlyFromThatAssemblies = config?.RegisterInterfacesOnlyFromThatAssemblies ?? Array.Empty<string>();
                settings.ScanAssemblies = config?.ScanAssemblies ?? Array.Empty<string>();
                settings.SplitGeneratedFiles = config?.SplitGeneratedFiles ?? false;
            }
        }

        return settings;
    }

    /// <summary>
    /// Extracts a group name from a pattern or type name.
    /// E.g., "*Page" -> "Pages", "*ViewModel" -> "ViewModels", "*CommandBuilder" -> "CommandBuilders"
    /// Note: "*Service" becomes "ServiceTypes" to avoid conflict with main RegisterServices method.
    /// </summary>
    public static string GetGroupNameFromPattern(string pattern)
    {
        // Remove wildcards and get the suffix
        var suffix = pattern.TrimStart('*').TrimEnd('*');
        if (string.IsNullOrEmpty(suffix))
            return "Other";

        // Special case: "Service" -> "ServiceTypes" to avoid conflict with main RegisterServices method
        if (suffix == "Service")
            return "ServiceTypes";

        // Pluralize simple cases
        if (suffix.EndsWith("y") && !suffix.EndsWith("ey") && !suffix.EndsWith("ay") && !suffix.EndsWith("oy"))
            return suffix.Substring(0, suffix.Length - 1) + "ies";

        if (suffix.EndsWith("s") || suffix.EndsWith("x") || suffix.EndsWith("ch") || suffix.EndsWith("sh"))
            return suffix + "es";

        return suffix + "s";
    }

    /// <summary>
    /// Determines which group a type belongs to based on RegisterTypesMatching patterns.
    /// </summary>
    public string GetGroupForType(string typeName)
    {
        foreach (var pattern in RegisterTypesMatching)
        {
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            if (System.Text.RegularExpressions.Regex.IsMatch(typeName, regexPattern))
            {
                return GetGroupNameFromPattern(pattern);
            }
        }
        return "Other";
    }
}
