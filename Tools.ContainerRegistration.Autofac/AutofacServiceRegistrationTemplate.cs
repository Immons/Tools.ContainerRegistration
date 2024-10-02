using System.Linq;
using Tools.ContainerRegistration.Common.Generators.Interfaces;

namespace Tools.ContainerRegistration.Autofac;

public static class AutofacServiceRegistrationTemplate
{
    public static string GenerateServiceRegistration(
        IGenerator generator,
        AutofacServiceRegistration autofacServiceRegistration)
    {
        var stringUsings = autofacServiceRegistration.Usings.Select(u => $"using {u};").Aggregate(
            (
                    s,
                    s1) => $"{s}\n{s1}");
        
        var servicesBlock = autofacServiceRegistration.Entities.Select(e => generator.Generate(e).Entity).Aggregate(
            (
                    s,
                    s1) => $"{s}\n{s1}");
        
        var template = $@"
{stringUsings}
namespace {autofacServiceRegistration.Namespace};

public static class {autofacServiceRegistration.ContainerName}_GeneratedServiceRegistration
{{
    public static void RegisterServices({autofacServiceRegistration.ContainerType} builder)
    {{
        {servicesBlock}
    }}
}}";
        return template;
    }
}