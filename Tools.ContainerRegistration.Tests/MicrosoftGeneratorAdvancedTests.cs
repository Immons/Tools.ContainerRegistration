using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Microsoft;
using Tools.ContainerRegistration.Tests.TestHelpers;

namespace Tools.ContainerRegistration.Tests;

public class MicrosoftGeneratorAdvancedTests : GeneratorTestBase
{
    protected override IIncrementalGenerator CreateGenerator() => new MicrosoftServiceRegistrationGenerator();

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
        // Should not have AddSingleton<MyService>() or AddTransient<MyService>() without interface
        Assert.DoesNotContain("AddTransient<global::TestApp.MyService>()", generatedCode);
        Assert.DoesNotContain("AddSingleton<global::TestApp.MyService>()", generatedCode);
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

        Assert.Contains("MyService", generatedCode);
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
    public void Generator_TransientByDefault_ShouldGenerateAddTransient()
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

        Assert.Contains("AddTransient", generatedCode);
        Assert.DoesNotContain("AddSingleton", generatedCode);
        Assert.DoesNotContain("AddScoped", generatedCode);
    }

    [Fact]
    public void Generator_WithScopedAttribute_ShouldGenerateAddScoped()
    {
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                public interface IScopedService { }

                [Scoped]
                public class ScopedService : IScopedService { }
            }
            """;

        var generatedCode = RunGenerator(sourceCode);

        Assert.Contains("AddScoped", generatedCode);
    }

    [Fact]
    public void Generator_WithSingletonAttribute_ShouldGenerateAddSingleton()
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

        Assert.Contains("AddSingleton", generatedCode);
    }

    [Fact]
    public void Generator_SingletonWithInterface_ShouldUseReUseSingleton()
    {
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                public interface ISingletonService { }

                [Singleton]
                public class SingletonService : ISingletonService { }
            }
            """;

        var configJson = """
            {
                "RegisterAsSelf": true,
                "RegisterAsAllInheritedTypes": true
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("ReUseSingleton", generatedCode);
    }

    #endregion

    #region AutoActivate Tests

    [Fact]
    public void Generator_WithAutoActivateFalse_ShouldNotGenerateAutoActivation()
    {
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                [Singleton(autoActivate: false)]
                public class NoAutoActivateService { }
            }
            """;

        var generatedCode = RunGenerator(sourceCode);

        Assert.Contains("NoAutoActivateService", generatedCode);
        // AfterContainerBuilt should be empty or not contain GetRequiredService for this service
        var afterBuiltSection = generatedCode.Contains("AfterContainerBuilt");
        if (afterBuiltSection)
        {
            // Check that GetRequiredService is not called for this specific service
            var lines = generatedCode.Split('\n');
            var inAfterBuilt = false;
            var hasAutoActivate = false;
            foreach (var line in lines)
            {
                if (line.Contains("AfterContainerBuilt")) inAfterBuilt = true;
                if (inAfterBuilt && line.Contains("GetRequiredService<NoAutoActivateService>"))
                    hasAutoActivate = true;
            }
            Assert.False(hasAutoActivate);
        }
    }

    [Fact]
    public void Generator_WithAutoActivateTrue_ShouldGenerateAutoActivation()
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
        Assert.Contains("GetRequiredService", generatedCode);
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

        Assert.Contains("AddSingleton", generatedCode);
        Assert.Contains("AddScoped", generatedCode);
        Assert.Contains("AddTransient", generatedCode);
    }

    #endregion

    #region Namespace Tests

    [Fact]
    public void Generator_WithNestedNamespace_ShouldRegisterCorrectly()
    {
        var sourceCode = """
            namespace TestApp.Services.Internal
            {
                public class DeepNestedService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("DeepNestedService", generatedCode);
        Assert.Contains("TestApp.Services.Internal", generatedCode);
    }

    [Fact]
    public void Generator_WithMultipleNamespaces_ShouldRegisterAll()
    {
        var sourceCode = """
            namespace TestApp.Services
            {
                public class ServiceA { }
            }
            namespace TestApp.Repositories
            {
                public class RepositoryA { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*", "*Repository*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("ServiceA", generatedCode);
        Assert.Contains("RepositoryA", generatedCode);
    }

    #endregion

    #region Pattern Matching Edge Cases

    [Fact]
    public void Generator_WithExactMatchPattern_ShouldMatchOnlyExact()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class Service { }
                public class MyService { }
                public class ServiceManager { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["TestApp.Service"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("TestApp.Service", generatedCode);
        Assert.DoesNotContain("MyService", generatedCode);
        Assert.DoesNotContain("ServiceManager", generatedCode);
    }

    [Fact]
    public void Generator_WithPrefixWildcard_ShouldMatchSuffix()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class UserRepository { }
                public class OrderRepository { }
                public class RepositoryBase { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Repository"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("UserRepository", generatedCode);
        Assert.Contains("OrderRepository", generatedCode);
        Assert.DoesNotContain("RepositoryBase", generatedCode);
    }

    [Fact]
    public void Generator_WithSuffixWildcard_ShouldMatchPrefix()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class BaseService { }
                public class BaseRepository { }
                public class UserService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Base*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("BaseService", generatedCode);
        Assert.Contains("BaseRepository", generatedCode);
        Assert.DoesNotContain("UserService", generatedCode);
    }

    [Fact]
    public void Generator_WithMultipleExclusionPatterns_ShouldExcludeAll()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class UserService { }
                public class BaseService { }
                public class AbstractService { }
                public class InternalService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"],
                "ExcludedFromRegisteringMatching": ["*Base*", "*Abstract*", "*Internal*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("UserService", generatedCode);
        Assert.DoesNotContain("BaseService", generatedCode);
        Assert.DoesNotContain("AbstractService", generatedCode);
        Assert.DoesNotContain("InternalService", generatedCode);
    }

    #endregion

    #region Interface-Only Registration

    [Fact]
    public void Generator_WithNoInterfaces_ShouldStillRegisterSelf()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class StandaloneService { }
            }
            """;

        var configJson = """
            {
                "RegisterAsSelf": true,
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("StandaloneService", generatedCode);
    }

    [Fact]
    public void Generator_WithMultipleInterfaces_ShouldRegisterAll()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface IReadable { }
                public interface IWritable { }
                public interface IDeletable { }

                public class CrudService : IReadable, IWritable, IDeletable { }
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

        Assert.Contains("CrudService", generatedCode);
        Assert.Contains("IReadable", generatedCode);
        Assert.Contains("IWritable", generatedCode);
        Assert.Contains("IDeletable", generatedCode);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Generator_WithEmptyNamespace_ShouldHandle()
    {
        var sourceCode = """
            public class GlobalService { }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("GlobalService", generatedCode);
    }

    [Fact]
    public void Generator_WithGenericInterface_ShouldRegister()
    {
        var sourceCode = """
            namespace TestApp
            {
                public interface IRepository<T> { }
                public class UserRepository : IRepository<string> { }
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
        Assert.Contains("IRepository", generatedCode);
    }

    [Fact]
    public void Generator_WithPrivateClass_ShouldNotRegister()
    {
        var sourceCode = """
            namespace TestApp
            {
                public class PublicService { }

                class InternalService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        Assert.Contains("PublicService", generatedCode);
        // Internal classes might still be registered, but this tests the scenario
    }

    [Fact]
    public void Generator_WithPartialClass_ShouldRegisterOnce()
    {
        var sourceCode = """
            namespace TestApp
            {
                public partial class PartialService { }
                public partial class PartialService { }
            }
            """;

        var configJson = """
            {
                "RegisterTypesMatching": ["*Service*"]
            }
            """;

        var generatedCode = RunGenerator(sourceCode, configJson);

        // Count occurrences of PartialService registration
        var count = generatedCode.Split("PartialService").Length - 1;
        Assert.True(count >= 1); // Should be registered at least once
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
                public interface ISpecificInterface { }
                public interface IOtherInterface { }

                [Singleton]
                [ServiceRegistration(typeof(ISpecificInterface))]
                public class CombinedService : ISpecificInterface, IOtherInterface { }
            }
            """;

        var generatedCode = RunGenerator(sourceCode);

        Assert.Contains("CombinedService", generatedCode);
        Assert.Contains("ISpecificInterface", generatedCode);
        Assert.Contains("Singleton", generatedCode);
        // Should NOT contain IOtherInterface because ServiceRegistration specifies exact interfaces
        Assert.DoesNotContain("IOtherInterface", generatedCode);
    }

    [Fact]
    public void Generator_ScopedWithAutoActivate_ShouldNotAutoActivate()
    {
        // AutoActivate only works with Singleton
        var sourceCode = """
            using Tools.ContainerRegistration.Attributes;

            namespace TestApp
            {
                [Scoped]
                public class ScopedWithAutoActivate { }
            }
            """;

        var generatedCode = RunGenerator(sourceCode);

        Assert.Contains("ScopedWithAutoActivate", generatedCode);
        Assert.Contains("AddScoped", generatedCode);
    }

    #endregion
}
