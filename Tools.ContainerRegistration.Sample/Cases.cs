using System.Globalization;
using Autofac;
using Tools.ContainerRegistration.Attributes;

namespace Tools.ContainerRegistration.Sample;

/// <summary>
/// will be registered because in ioc_config.json we have "Page" in "RegisterTypesEndingWith",
/// but only as TestPage because Page is excluded in "ExcludedFromRegisteringAsConventionInterface"
/// </summary>
[Singleton]
public class TestPage : ITestPage
{
}

public interface ITestPage
{
}

/// <summary>
/// Wont be registered at all, because although it contains Singleton attribute and exist in "RegisterTypesEndingWith"
/// it also contains ManualRegistration attribute
/// </summary>
[ManualRegistration]
[Singleton]
public class TestUserPage : ITestUserPage
{
}

public interface ITestUserPage
{
}

/// <summary>
/// will not be registered because it exist in ioc_config.json in "ExcludedFromRegisteringTypesEndingWith"
/// </summary>
public class EnemyUserViewModel
{
}

/// <summary>
/// will be registered although it exist in in ioc_config.json in "ExcludedFromRegisteringTypesEndingWith",
/// because it contains attribute
/// It will also be registered as IBaseUserViewModel and IFriendsViewModel due to ServiceRegistration attribute
/// </summary>
[Singleton]
[ServiceRegistration([typeof(IBaseUserViewModel), typeof(IFriendsViewModel)])]
public class FriendlyUserViewModel : IBaseUserViewModel, IFriendsViewModel
{
}

public interface IBaseUserViewModel
{
}

public interface IFriendsViewModel
{
}

//will be registered as ProfileViewModel because exist in ioc_config.json in "RegisterTypesEndingWith"
//but won't be registered as IProfileViewModel because it also exists in "ExcludedFromRegisteringAsConventionInterface"
public class ProfileViewModel : IProfileViewModel
{
}

public interface IProfileViewModel
{
}

/// <summary>
/// Factory should accept 2 parameters - Type and ServiceProvider where ServiceProvider can be either
/// IServiceProvider for Microsoft either
/// IComponentContext for Autofac
/// </summary>
[FactoryRegistration("Tools.ContainerRegistration.Sample.HelloWorldServiceFactory.CreateHelloWorldService")]
[Singleton(true)]
public interface IHelloWorldService
{
    void SayHello();
}

[ManualRegistration]
public class HelloWorldService : IHelloWorldService
{
    private readonly CultureInfo _currentCulture;

    public HelloWorldService(
        CultureInfo currentCulture)
    {
        _currentCulture = currentCulture;
        SayHello();
    }

    public void SayHello()
    {
        Console.WriteLine($"I would like to say Hello World in {_currentCulture.EnglishName}, but I don't know it!");
    }
}

public static class HelloWorldServiceFactory
{
    public static object CreateHelloWorldService(Type typeToCreate, IServiceProvider provider) => new HelloWorldService(CultureInfo.CurrentUICulture);
    public static object CreateHelloWorldService(Type typeToCreate, IComponentContext provider) => new HelloWorldService(CultureInfo.CurrentUICulture);
}