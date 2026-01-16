using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Tools.ContainerRegistration.Common;

namespace Tools.ContainerRegistration.Tests;

public class GlobalSettingsTests
{
    [Fact]
    public void LoadSettings_WithoutConfigFile_ShouldUseDefaults()
    {
        // Arrange
        var additionalTexts = ImmutableArray<AdditionalText>.Empty;

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.True(settings.RegisterAsSelf);
        Assert.True(settings.RegisterAsAllInheritedTypes);
        Assert.True(settings.RegisterAsDirectlyInheritedTypes);
        Assert.Empty(settings.RegisterInterfacesOnlyFromThatAssemblies);
        Assert.Empty(settings.ExcludedFromRegisteringMatching);
        Assert.Contains("*Service*", settings.RegisterTypesMatching);
        Assert.Contains("*Repository*", settings.RegisterTypesMatching);
    }

    [Fact]
    public void LoadSettings_WithConfigFile_ShouldLoadSettings()
    {
        // Arrange
        var configJson = """
        {
            "RegisterAsSelf": false,
            "RegisterAsAllInheritedTypes": false,
            "RegisterAsDirectlyInheritedTypes": false,
            "RegisterTypesMatching": ["*Custom*"],
            "ExcludedFromRegisteringMatching": ["*Excluded*"],
            "RegisterInterfacesOnlyFromThatAssemblies": ["TestAssembly"]
        }
        """;
        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.json", configJson));

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.False(settings.RegisterAsSelf);
        Assert.False(settings.RegisterAsAllInheritedTypes);
        Assert.False(settings.RegisterAsDirectlyInheritedTypes);
        Assert.Single(settings.RegisterTypesMatching);
        Assert.Contains("*Custom*", settings.RegisterTypesMatching);
        Assert.Single(settings.ExcludedFromRegisteringMatching);
        Assert.Contains("*Excluded*", settings.ExcludedFromRegisteringMatching);
        Assert.Single(settings.RegisterInterfacesOnlyFromThatAssemblies);
        Assert.Contains("TestAssembly", settings.RegisterInterfacesOnlyFromThatAssemblies);
    }

    [Fact]
    public void LoadSettings_MultipleCallsWithDifferentConfigs_ShouldReturnIndependentInstances()
    {
        // Arrange - first load with custom settings
        var configJson = """
        {
            "RegisterAsSelf": false,
            "RegisterTypesMatching": ["*Custom*"],
            "ExcludedFromRegisteringMatching": ["*Excluded*"]
        }
        """;
        var additionalTextsWithConfig = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.json", configJson));
        var settings1 = GlobalSettings.LoadSettings(additionalTextsWithConfig, CancellationToken.None);

        // Verify custom settings were applied
        Assert.False(settings1.RegisterAsSelf);
        Assert.Contains("*Custom*", settings1.RegisterTypesMatching);

        // Act - second load without config file
        var additionalTextsEmpty = ImmutableArray<AdditionalText>.Empty;
        var settings2 = GlobalSettings.LoadSettings(additionalTextsEmpty, CancellationToken.None);

        // Assert - settings2 should have defaults (independent of settings1)
        Assert.True(settings2.RegisterAsSelf);
        Assert.Contains("*Service*", settings2.RegisterTypesMatching);
        Assert.DoesNotContain("*Custom*", settings2.RegisterTypesMatching);
        Assert.Empty(settings2.ExcludedFromRegisteringMatching);

        // Verify settings1 is still unchanged
        Assert.False(settings1.RegisterAsSelf);
        Assert.Contains("*Custom*", settings1.RegisterTypesMatching);
    }

    [Fact]
    public void LoadSettings_WithPartialConfig_ShouldUseDefaultsForMissingValues()
    {
        // Arrange - config with only some values
        var configJson = """
        {
            "RegisterAsSelf": false
        }
        """;
        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.json", configJson));

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.False(settings.RegisterAsSelf); // from config
        Assert.True(settings.RegisterAsAllInheritedTypes); // default
        Assert.Contains("*Service*", settings.RegisterTypesMatching); // default
    }

    [Fact]
    public void DefaultPatterns_ShouldContainWildcards()
    {
        // Arrange
        var additionalTexts = ImmutableArray<AdditionalText>.Empty;

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert - all default patterns should have wildcards
        foreach (var pattern in settings.RegisterTypesMatching)
        {
            Assert.Contains("*", pattern);
        }
    }

    [Fact]
    public void LoadSettings_ReturnsNewInstanceEachTime()
    {
        // Arrange
        var additionalTexts = ImmutableArray<AdditionalText>.Empty;

        // Act
        var settings1 = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);
        var settings2 = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert - should be different instances
        Assert.NotSame(settings1, settings2);
    }

    [Fact]
    public void LoadSettings_WithEmptyArrays_ShouldLoadEmptyArrays()
    {
        // Arrange
        var configJson = """
        {
            "RegisterTypesMatching": [],
            "ExcludedFromRegisteringMatching": [],
            "RegisterInterfacesOnlyFromThatAssemblies": []
        }
        """;
        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.json", configJson));

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.Empty(settings.RegisterTypesMatching);
        Assert.Empty(settings.ExcludedFromRegisteringMatching);
        Assert.Empty(settings.RegisterInterfacesOnlyFromThatAssemblies);
    }

    [Fact]
    public void LoadSettings_WithAllBoolsFalse_ShouldLoadFalseValues()
    {
        // Arrange
        var configJson = """
        {
            "RegisterAsSelf": false,
            "RegisterAsAllInheritedTypes": false,
            "RegisterAsDirectlyInheritedTypes": false
        }
        """;
        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.json", configJson));

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.False(settings.RegisterAsSelf);
        Assert.False(settings.RegisterAsAllInheritedTypes);
        Assert.False(settings.RegisterAsDirectlyInheritedTypes);
    }

    [Fact]
    public void LoadSettings_WithSpecialCharactersInPatterns_ShouldLoad()
    {
        // Arrange
        var configJson = """
        {
            "RegisterTypesMatching": ["*.Internal.*", "*_Test*", "*+Nested*"]
        }
        """;
        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.json", configJson));

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.Contains("*.Internal.*", settings.RegisterTypesMatching);
        Assert.Contains("*_Test*", settings.RegisterTypesMatching);
        Assert.Contains("*+Nested*", settings.RegisterTypesMatching);
    }

    [Fact]
    public void LoadSettings_WithManyAssemblies_ShouldLoadAll()
    {
        // Arrange
        var configJson = """
        {
            "RegisterInterfacesOnlyFromThatAssemblies": [
                "Assembly1",
                "Assembly2",
                "Assembly3",
                "My.Long.Assembly.Name"
            ]
        }
        """;
        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.json", configJson));

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.Equal(4, settings.RegisterInterfacesOnlyFromThatAssemblies.Length);
        Assert.Contains("Assembly1", settings.RegisterInterfacesOnlyFromThatAssemblies);
        Assert.Contains("My.Long.Assembly.Name", settings.RegisterInterfacesOnlyFromThatAssemblies);
    }

    [Fact]
    public void LoadSettings_ConfigFileNotEndingWithJson_ShouldUseDefaults()
    {
        // Arrange - file with wrong extension
        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.txt", "{ }"));

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert - should use defaults
        Assert.True(settings.RegisterAsSelf);
        Assert.Contains("*Service*", settings.RegisterTypesMatching);
    }

    [Fact]
    public void AttributeNames_ShouldBeCorrect()
    {
        // Assert - verify constant names
        Assert.Equal("SingletonAttribute", GlobalSettings.SingletonAttribute);
        Assert.Equal("ScopedAttribute", GlobalSettings.ScopedAttribute);
        Assert.Equal("ManualRegistrationAttribute", GlobalSettings.ManualRegistrationAttribute);
        Assert.Equal("ServiceRegistrationAttribute", GlobalSettings.ServiceRegistrationAttribute);
        Assert.Equal("FactoryRegistrationAttribute", GlobalSettings.FactoryRegistrationAttribute);
    }

    [Fact]
    public void DefaultPatterns_ShouldContainExpectedPatterns()
    {
        // Arrange
        var additionalTexts = ImmutableArray<AdditionalText>.Empty;

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.Contains("*Service*", settings.RegisterTypesMatching);
        Assert.Contains("*Repository*", settings.RegisterTypesMatching);
        Assert.Contains("*Factory*", settings.RegisterTypesMatching);
        Assert.Contains("*Map*", settings.RegisterTypesMatching);
        Assert.Contains("*ViewModel*", settings.RegisterTypesMatching);
        Assert.Contains("*Page*", settings.RegisterTypesMatching);
        Assert.Contains("*Action*", settings.RegisterTypesMatching);
        Assert.Contains("*CommandBuilder*", settings.RegisterTypesMatching);
    }

    [Fact]
    public void LoadSettings_WithScanAssemblies_ShouldLoad()
    {
        // Arrange
        var configJson = """
        {
            "ScanAssemblies": ["Assembly1", "Assembly2"]
        }
        """;
        var additionalTexts = ImmutableArray.Create<AdditionalText>(
            new InMemoryAdditionalText("ioc_config.json", configJson));

        // Act
        var settings = GlobalSettings.LoadSettings(additionalTexts, CancellationToken.None);

        // Assert
        Assert.Equal(2, settings.ScanAssemblies.Length);
        Assert.Contains("Assembly1", settings.ScanAssemblies);
        Assert.Contains("Assembly2", settings.ScanAssemblies);
    }

    private class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            _text = text;
        }

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(_text);
        }
    }
}
