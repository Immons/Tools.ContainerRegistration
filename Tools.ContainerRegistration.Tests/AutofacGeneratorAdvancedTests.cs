using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Autofac;
using Tools.ContainerRegistration.Tests.TestHelpers;

namespace Tools.ContainerRegistration.Tests;

public class AutofacGeneratorAdvancedTests : GeneratorTestBase
{
    protected override IIncrementalGenerator CreateGenerator() => new AutofacServiceRegistrationGenerator();

    #region RegisterAsSelf Tests

    [Fact]
    public void Generator_WithRegisterAsSelfFalse_ShouldNotRegisterSelf()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface IService { }
                public class MyService : IService { }
            }
            """;

        var configJson = """
            {
                "RegisterAsSelf": false,
                "RegisterAsAllInheritedTypes": true,
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("IService", generatedCode);
        // Should contain As<IService> but not AsSelf
        Assert.DoesNotContain(".AsSelf()", generatedCode);
    }

    [Fact]
    public void Generator_WithRegisterAsSelfTrue_ShouldRegisterSelf()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface IService { }
                public class MyService : IService { }
            }
            """;

        var configJson = """
            {
                "RegisterAsSelf": true,
                "RegisterAsAllInheritedTypes": true,
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("AsSelf", generatedCode);
    }

    #endregion

    #region Interface Inheritance Tests

    [Fact]
    public void Generator_WithRegisterAsAllInheritedTypes_ShouldRegisterAllInterfaces()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface IBase { }
                public interface IDerived : IBase { }
                public class MyService : IDerived { }
            }
            """;

        var configJson = """
            {
                "RegisterAsSelf": false,
                "RegisterAsAllInheritedTypes": true,
                "RegisterAsDirectlyInheritedTypes": false,
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("IDerived", generatedCode);
        Assert.Contains("IBase", generatedCode);
    }

    [Fact]
    public void Generator_WithRegisterAsDirectlyInheritedTypesOnly_ShouldRegisterOnlyDirectInterfaces()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface IBase { }
                public interface IDerived : IBase { }
                public class MyService : IDerived { }
            }
            """;

        var configJson = """
            {
                "RegisterAsSelf": false,
                "RegisterAsAllInheritedTypes": false,
                "RegisterAsDirectlyInheritedTypes": true,
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("IDerived", generatedCode);
        Assert.DoesNotContain("IBase", generatedCode);
    }

    #endregion

    #region Scope Tests

    [Fact]
    public void Generator_TransientByDefault_ShouldNotHaveLifetimeSpecifier()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class TransientService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("RegisterType", generatedCode);
        Assert.DoesNotContain("SingleInstance", generatedCode);
        Assert.DoesNotContain("InstancePerLifetimeScope", generatedCode);
    }

    [Fact]
    public void Generator_WithScopedAttribute_ShouldGenerateInstancePerLifetimeScope()
    {
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                [Scoped]
                public class ScopedService { }
            }
            """;

        var generatedCode = RunGenerator(sourceCode);

        Assert.Contains("InstancePerLifetimeScope", generatedCode);
    }

    [Fact]
    public void Generator_WithSingletonAttribute_ShouldGenerateSingleInstance()
    {
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                [Singleton]
                public class SingletonService { }
            }
            """;

        var generatedCode = RunGenerator(sourceCode);

        Assert.Contains("SingleInstance", generatedCode);
    }

    #endregion

    #region AutoActivate Tests

    [Fact]
    public void Generator_WithAutoActivateTrue_ShouldGenerateAutoActivate()
    {
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                [Singleton(autoActivate: true)]
                public class AutoActivateService { }
            }
            """;

        var generatedCode = RunGenerator(sourceCode);

        Assert.Contains("AutoActivateService", generatedCode);
        Assert.Contains("AutoActivate", generatedCode);
    }

    #endregion

    #region Multiple Classes Tests

    [Fact]
    public void Generator_WithMultipleServices_ShouldRegisterAll()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class UserService { }
                public class OrderService { }
                public class ProductService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("UserService", generatedCode);
        Assert.Contains("OrderService", generatedCode);
        Assert.Contains("ProductService", generatedCode);
    }

    [Fact]
    public void Generator_WithMixedScopes_ShouldGenerateCorrectly()
    {
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                [Singleton]
                public class SingletonService { }

                [Scoped]
                public class ScopedService { }

                public class TransientService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("SingleInstance", generatedCode);
        Assert.Contains("InstancePerLifetimeScope", generatedCode);
    }

    #endregion

    #region Autofac-Specific Tests

    [Fact]
    public void Generator_ShouldUseContainerBuilder()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class TestService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("ContainerBuilder", generatedCode);
    }

    [Fact]
    public void Generator_ShouldGenerateAsInterface()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface ITestService { }
                public class TestService : ITestService { }
            }
            """;

        var configJson = """
            {
                "RegisterAsAllInheritedTypes": true,
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains(".As<", generatedCode);
    }

    [Fact]
    public void Generator_WithMultipleInterfaces_ShouldChainAs()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface IFirst { }
                public interface ISecond { }

                public class MultiService : IFirst, ISecond { }
            }
            """;

        var configJson = """
            {
                "RegisterAsSelf": true,
                "RegisterAsAllInheritedTypes": true,
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("IFirst", generatedCode);
        Assert.Contains("ISecond", generatedCode);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void Generator_WithMultiplePatterns_ShouldMatchAll()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class UserService { }
                public class UserRepository { }
                public class UserFactory { }
                public class UserHelper { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*", "*Repository*", "*Factory*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("UserService", generatedCode);
        Assert.Contains("UserRepository", generatedCode);
        Assert.Contains("UserFactory", generatedCode);
        Assert.DoesNotContain("UserHelper", generatedCode);
    }

    [Fact]
    public void Generator_WithExclusionPatterns_ShouldExcludeMatches()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class RealService { }
                public class MockService { }
                public class FakeService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"],
                "ExcludedFromRegisteringMatching": ["*Mock*", "*Fake*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("RealService", generatedCode);
        Assert.DoesNotContain("MockService", generatedCode);
        Assert.DoesNotContain("FakeService", generatedCode);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Generator_WithGenericClass_ShouldRegister()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface IRepository<T> { }
                public class GenericRepository<T> : IRepository<T> { }
                public class UserRepository : GenericRepository<string> { }
            }
            """;

        var configJson = """
            {
                "RegisterAsAllInheritedTypes": true,
                "RegisterTypesMatching": ["*Repository*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("UserRepository", generatedCode);
    }

    [Fact]
    public void Generator_WithNestedClass_ShouldRegister()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class Outer
                {
                    public class NestedService { }
                }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("NestedService", generatedCode);
    }

    [Fact]
    public void Generator_WithSealedClass_ShouldRegister()
    {
        var sourceCode = """
            namespace TestApp
            {
                public sealed class SealedService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("SealedService", generatedCode);
    }

    #endregion

    #region Combined Attributes

    [Fact]
    public void Generator_SingletonWithServiceRegistration_ShouldCombine()
    {
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                public interface ISpecific { }
                public interface IOther { }

                [Singleton]
                [ServiceRegistration(typeof(ISpecific))]
                public class CombinedService : ISpecific, IOther { }
            }
            """;

        var generatedCode = RunGenerator(sourceCode);

        Assert.Contains("CombinedService", generatedCode);
        Assert.Contains("ISpecific", generatedCode);
        Assert.Contains("SingleInstance", generatedCode);
        Assert.DoesNotContain("IOther", generatedCode);
    }

    #endregion
}
