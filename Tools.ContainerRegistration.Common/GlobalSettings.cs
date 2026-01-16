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
            }
        }

        return settings;
    }
}
