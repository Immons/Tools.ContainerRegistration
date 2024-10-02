# Tools.ContainerRegistration

This project demonstrates how to register various services using Autofac or Microsoft DI, based on conventions and attributes. The registration configuration is governed by rules specified in `ioc_config.json` and custom attributes such as `[Singleton]`, `[Scoped]`, `[ManualRegistration]`, and `[ServiceRegistration]`.

## Table of Contents

1. [Overview](#overview)
2. [Attributes](#attributes)
    - [Singleton](#singleton)
    - [Scoped](#scoped)
    - [ManualRegistration](#manualregistration)
    - [ServiceRegistration](#serviceregistration)
    - [FactoryRegistration](#factoryregistration)
3. [Service Factory](#service-factory)
4. [Registration Rules](#registration-rules)
5. [Examples](#examples)
6. [Generation output](#generation-output)
   - [Autofac](#autofac)
   - [Microsoft DI](#microsoft-dependency-injection)
---

## Overview

The goal of this project is to manage service registration based on type conventions, interfaces, and custom attributes. The `ioc_config.json` file defines what types should be automatically registered, while the attributes fine-tune how specific types and interfaces should be treated.

Some services will be automatically registered based on naming conventions (such as ending with "Page" or "ViewModel"), while others require explicit attribute markings.

Not all namespaces are always resolved automatically, so the best approach to work with this tooling is to create GlobalUsings.cs and list all needed namespaces

```csharp
global using Project.Mobile.Commands.Base.Interfaces;
global using Project.Mobile.Pages;
global using Project.Mobile.Pages.Base;
global using Project.Mobile.ViewModels;
global using Project.Mobile.ViewModels.Interfaces;
```

---

## Attributes

### Singleton

The `[Singleton]` attribute marks a class for registration as a singleton service. If the class is automatically registered based on naming conventions, this attribute ensures it is treated as a singleton regardless of other potential scope attributes.

**Example:**
```csharp
[Singleton]
public class TestPage : ITestPage
{
    // This class will be registered as a singleton.
}
```

### Scoped

The `[Scoped]` attribute indicates that a class should be registered with a scoped lifetime. However, if the `[Singleton]` attribute is also present, the singleton takes precedence.

**Example:**
```csharp
[Scoped]
[Singleton(true)]
public class TestService : ITestService
{
    // Although it has both attributes, Singleton(true) ensures it is a singleton.
}
```

### ManualRegistration

Classes with the `[ManualRegistration]` attribute are excluded from automatic registration. These services must be manually registered in the DI container.

**Example:**
```csharp
[ManualRegistration]
public class TestUserPage : ITestUserPage
{
    // This class will not be registered automatically due to ManualRegistration.
}
```

### ServiceRegistration

The `[ServiceRegistration]` attribute allows for manual control over which interfaces a class should be registered as. This is useful when a class implements multiple interfaces, but only some of them need to be registered.

**Example:**
```csharp
[Singleton]
[ServiceRegistration([typeof(IBaseUserViewModel), typeof(IFriendsViewModel)])]
public class FriendlyUserViewModel : IBaseUserViewModel, IFriendsViewModel
{
    // Will be registered as IBaseUserViewModel and IFriendsViewModel, but not as FriendlyUserViewModel.
}
```

### FactoryRegistration

The `[FactoryRegistration]` attribute is used to register a service via a factory method. This allows for custom service creation, where the factory method accepts a `Type` and a service provider (either `IServiceProvider` or `IComponentContext` for Autofac).

**Example:**

```csharp
[FactoryRegistration("Tools.ContainerRegistration.Sample.HelloWorldServiceFactory.CreateHelloWorldService")]
[Singleton(true)]
public interface IHelloWorldService
{
    void SayHello();
}
```

---

## Registration Rules

### Based on `ioc_config.json`

- **RegisterTypesEndingWith:** Automatically registers types whose names end with specified suffixes, such as "Page" or "ViewModel".
- **ExcludedFromRegisteringTypesEndingWith:** Excludes types from registration even if they match the suffix rules.
- **ExcludedFromRegisteringAsConventionInterface:** Prevents automatic registration of a class as an interface based on convention.
- **RegisterAsSelf:** If true service will be always registered as self (for example: TestPage will be registered as TestPage)

---

## Examples

### Automatically Registered Services

- **TestPage** is registered as a singleton because it ends with "Page" and the `ioc_config.json` includes "Page" in `RegisterTypesEndingWith`. However, it will not be registered as `ITestPage` due to the `ExcludedFromRegisteringAsConventionInterface` rule.

```csharp
[Singleton]
public class TestPage : ITestPage
{
}
```

### Excluded from Registration

- **EnemyUserViewModel** is excluded from automatic registration as it is listed in `ExcludedFromRegisteringTypesEndingWith` in `ioc_config.json`.

```csharp
public class EnemyUserViewModel
{
}
```

### Custom Service Registration

- **FriendlyUserViewModel** is registered as both `IBaseUserViewModel` and `IFriendsViewModel`, bypassing the exclusion rule due to the presence of the `[ServiceRegistration]` attribute.

```csharp
[Singleton]
[ServiceRegistration([typeof(IBaseUserViewModel), typeof(IFriendsViewModel)])]
public class FriendlyUserViewModel : IBaseUserViewModel, IFriendsViewModel
{
}
```

### Factory Registration

**Example:**

```csharp

[FactoryRegistration("Tools.ContainerRegistration.Sample.HelloWorldServiceFactory.CreateHelloWorldService")]
[Singleton(true)]
public interface IHelloWorldService
{
    void SayHello();
}

```

where factory and implementation is

```csharp
namespace Tools.ContainerRegistration.Sample;

public static class HelloWorldServiceFactory
{
    public static object CreateHelloWorldService(Type typeToCreate, IServiceProvider provider) => new HelloWorldService(CultureInfo.CurrentUICulture);
    public static object CreateHelloWorldService(Type typeToCreate, IComponentContext provider) => new HelloWorldService(CultureInfo.CurrentUICulture);
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

```

### Manual Registration

- **TestUserPage** is not registered automatically despite having the `[Singleton]` attribute, due to the `[ManualRegistration]` attribute. This service must be registered manually in the DI container.

```csharp
[ManualRegistration]
[Singleton]
public class TestUserPage : ITestUserPage
{
}
```

---

### Generation Output

### Autofac
When using **Autofac**, library will create for example such class:

```csharp
namespace Tools.ContainerRegistration.Sample;

public static class Autofac_GeneratedServiceRegistration
{
    public static void RegisterServices(ContainerBuilder builder)
    {
         builder.RegisterType<global::Tools.ContainerRegistration.Sample.TestPage>().AsSelf().SingleInstance();
         builder.RegisterType<global::Tools.ContainerRegistration.Sample.FriendlyUserViewModel>().As<Tools.ContainerRegistration.Sample.IBaseUserViewModel>().As<Tools.ContainerRegistration.Sample.IFriendsViewModel>().AsSelf().SingleInstance();
         builder.RegisterType<global::Tools.ContainerRegistration.Sample.ProfileViewModel>().AsSelf();
         builder.RegisterType<global::Tools.ContainerRegistration.Sample.TestService>().As<Tools.ContainerRegistration.Sample.ITestService>().As<Tools.ContainerRegistration.Sample.ITestService2>().AsSelf().SingleInstance().AutoActivate();
         builder.Register(context => Tools.ContainerRegistration.Sample.HelloWorldServiceFactory.CreateHelloWorldService(typeof(global::Tools.ContainerRegistration.Sample.IHelloWorldService), context)).As<IHelloWorldService>().AsSelf().SingleInstance().AutoActivate();
    }
}
```

then you can use it when initializing your container that way:

```csharp
var containerBuilder = new ContainerBuilder();
Autofac_GeneratedServiceRegistration.RegisterServices(containerBuilder);
containerBuilder.Build();
```

### Microsoft Dependency Injection

When using **Microsoft Dependency Injection**, library will create for example such class:

```csharp
namespace Tools.ContainerRegistration.Sample;

public static class ServiceCollection_GeneratedServiceRegistration
{
    public static void RegisterServices(IServiceCollection builder)
    {
         builder.AddSingleton<global::Tools.ContainerRegistration.Sample.TestPage>();
         Tools.ContainerRegistration.Microsoft.Extensions.ServiceCollectionExtension.ReUseSingleton<global::Tools.ContainerRegistration.Sample.FriendlyUserViewModel, Tools.ContainerRegistration.Sample.IBaseUserViewModel>(builder);
         Tools.ContainerRegistration.Microsoft.Extensions.ServiceCollectionExtension.ReUseSingleton<global::Tools.ContainerRegistration.Sample.FriendlyUserViewModel, Tools.ContainerRegistration.Sample.IFriendsViewModel>(builder);
         builder.AddSingleton<global::Tools.ContainerRegistration.Sample.FriendlyUserViewModel>();
         builder.AddTransient<global::Tools.ContainerRegistration.Sample.ProfileViewModel>();
         Tools.ContainerRegistration.Microsoft.Extensions.ServiceCollectionExtension.ReUseSingleton<global::Tools.ContainerRegistration.Sample.TestService, Tools.ContainerRegistration.Sample.ITestService>(builder);
         Tools.ContainerRegistration.Microsoft.Extensions.ServiceCollectionExtension.ReUseSingleton<global::Tools.ContainerRegistration.Sample.TestService, Tools.ContainerRegistration.Sample.ITestService2>(builder);
         builder.AddSingleton<global::Tools.ContainerRegistration.Sample.TestService>();
         builder.AddSingleton(typeof(global::Tools.ContainerRegistration.Sample.IHelloWorldService), provider => Tools.ContainerRegistration.Sample.HelloWorldServiceFactory.CreateHelloWorldService(typeof(global::Tools.ContainerRegistration.Sample.IHelloWorldService), provider));
    }

    public static void AfterContainerBuilt(IServiceProvider provider)
    {
         provider.GetRequiredService<TestService>();
         provider.GetRequiredService<IHelloWorldService>();
    }
}
```

then you can use it that way:

```csharp
var collection = new ServiceCollection();
ServiceCollection_GeneratedServiceRegistration.RegisterServices(collection);
var provider = collection.BuildServiceProvider();
ServiceCollection_GeneratedServiceRegistration.AfterContainerBuilt(provider);
```

take a look that it also generates **void AfterContainerBuilt(IServiceProvider provider)** method.
This method is used as way to AutoActivate services for Microsoft DI.

---

For further details, refer to the `ioc_config.json` file and attribute-based configurations in the source code.