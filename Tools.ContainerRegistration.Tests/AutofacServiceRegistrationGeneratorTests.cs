using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Tools.ContainerRegistration.Autofac;

namespace Tools.ContainerRegistration.Tests;

public class AutofacServiceRegistrationGeneratorTests
{
    [Fact]
    public void Generator_WithServiceClass_ShouldGenerateRegistration()
    {
        // Arrange
        var sourceCode = """
            namespace TestApp
            {
                public interface ITestService { }
                public class TestService : ITestService { }
            }
            """;

        var configJson = """
            {
                "RegisterAsSelf": true,
                "RegisterAsAllInheritedTypes": true,
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode, configJson);

        // Assert
        Assert.Contains("TestService", generatedCode);
        Assert.Contains("ITestService", generatedCode);
        Assert.Contains("RegisterType", generatedCode); // Autofac uses RegisterType
    }

    [Fact]
    public void Generator_WithSingletonAttribute_ShouldGenerateSingleInstanceRegistration()
    {
        // Arrange
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                public interface IMyComponent { }

                [Singleton]
                public class MyComponent : IMyComponent { }
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode);

        // Assert
        Assert.Contains("MyComponent", generatedCode);
        Assert.Contains("SingleInstance", generatedCode); // Autofac uses SingleInstance()
    }

    [Fact]
    public void Generator_WithScopedAttribute_ShouldGenerateScopedRegistration()
    {
        // Arrange
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                public interface IScopedComponent { }

                [Scoped]
                public class ScopedComponent : IScopedComponent { }
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode);

        // Assert
        Assert.Contains("ScopedComponent", generatedCode);
        Assert.Contains("InstancePerLifetimeScope", generatedCode); // Autofac uses InstancePerLifetimeScope()
    }

    [Fact]
    public void Generator_WithManualRegistrationAttribute_ShouldNotGenerateRegistration()
    {
        // Arrange
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                public interface IManualService { }

                [ManualRegistration]
                public class ManualService : IManualService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode, configJson);

        // Assert
        Assert.DoesNotContain("ManualService", generatedCode);
    }

    [Fact]
    public void Generator_WithStaticClass_ShouldNotGenerateRegistration()
    {
        // Arrange
        var sourceCode = """
            namespace TestApp
            {
                public static class StaticService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode, configJson);

        // Assert
        Assert.DoesNotContain("StaticService", generatedCode);
    }

    [Fact]
    public void Generator_WithExcludedPattern_ShouldNotGenerateRegistration()
    {
        // Arrange
        var sourceCode = """
            namespace TestApp
            {
                public class TestService { }
                public class BaseService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"],
                "ExcludedFromRegisteringMatching": ["*BaseService*"]
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode, configJson);

        // Assert
        Assert.Contains("TestService", generatedCode);
        Assert.DoesNotContain("BaseService", generatedCode);
    }

    [Fact]
    public void Generator_GeneratesAutofacSpecificCode()
    {
        // Arrange
        var sourceCode = """
            namespace TestApp
            {
                public interface IRepository { }
                public class UserRepository : IRepository { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Repository*"]
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode, configJson);

        // Assert
        Assert.Contains("ContainerBuilder", generatedCode); // Autofac container type
        Assert.Contains("Autofac_GeneratedServiceRegistration", generatedCode);
    }

    [Fact]
    public void Generator_WithAbstractClass_ShouldNotGenerateRegistration()
    {
        // Arrange
        var sourceCode = """
            namespace TestApp
            {
                public abstract class AbstractService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode, configJson);

        // Assert
        Assert.DoesNotContain("AbstractService", generatedCode);
    }

    [Fact]
    public void Generator_WithServiceRegistrationAttribute_ShouldRegisterSpecificInterfaces()
    {
        // Arrange
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                public interface IFirst { }
                public interface ISecond { }
                public interface IThird { }

                [ServiceRegistration(typeof(IFirst), typeof(ISecond))]
                public class MultiInterfaceComponent : IFirst, ISecond, IThird { }
            }
            """;

        // Act
        var generatedCode = RunGenerator(sourceCode);

        // Assert
        Assert.Contains("MultiInterfaceComponent", generatedCode);
        Assert.Contains("IFirst", generatedCode);
        Assert.Contains("ISecond", generatedCode);
    }

    private static string RunGenerator(string sourceCode, string? configJson = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Tools.ContainerRegistration.Attributes.SingletonAttribute).Assembly.Location)
        };

        // Add System.Runtime reference
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var systemRuntimePath = Path.Combine(runtimePath, "System.Runtime.dll");
        if (File.Exists(systemRuntimePath))
        {
            references.Add(MetadataReference.CreateFromFile(systemRuntimePath));
        }

        // Add netstandard reference (needed for netstandard2.0 assemblies like Attributes)
        var netstandardPath = Path.Combine(runtimePath, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AutofacServiceRegistrationGenerator();

        var additionalTexts = configJson != null
            ? ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("ioc_config.json", configJson))
            : ImmutableArray<AdditionalText>.Empty;

        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(additionalTexts);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("GeneratedServiceRegistration"))
            .ToList();

        return generatedTrees.Any()
            ? generatedTrees.First().GetText().ToString()
            : string.Empty;
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
