using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Tools.ContainerRegistration.Sample;

public static class Program
{
    static void Main(string[] args)
    {
        TestMicrosoftContainerRegistration();
        TestAutofacContainerRegistration();

        Console.ReadKey();
    }

    private static void TestMicrosoftContainerRegistration()
    {
        var collection = new ServiceCollection();
        ServiceCollection_GeneratedServiceRegistration.RegisterServices(collection);
        var provider = collection.BuildServiceProvider();
        ServiceCollection_GeneratedServiceRegistration.AfterContainerBuilt(provider);
    }

    private static void TestAutofacContainerRegistration()
    {
        var containerBuilder = new ContainerBuilder();
        Autofac_GeneratedServiceRegistration.RegisterServices(containerBuilder);
        containerBuilder.Build();
    }
}