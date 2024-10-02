using System.Linq;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Microsoft;

public static class MicrosoftServiceRegistrationTemplate
{
    public static string GenerateServiceRegistration(
        MicrosoftGenerator generator,
        MicrosoftServiceRegistration microsoftServiceRegistration)
    {
        var stringUsings = microsoftServiceRegistration.Usings.Select(u => $"using {u};").Aggregate(
            (
                    s,
                    s1) => $"{s}\n{s1}");

        var generatedEntities = microsoftServiceRegistration.Entities.Select(generator.Generate).OfType<MicrosoftGeneratedServiceRegistrationEntity>().ToList();
        
        var servicesBlock = generatedEntities.Select(e => e.Entity).Aggregate(
            (
                    s,
                    s1) => $"{s}\n{s1}");
        
        var fireAfterContainerBuilt = generatedEntities.Select(e => e.AutoActivate)?.Aggregate(
            (
                    s,
                    s1) => $"{s}\n{s1}");
        
        var template = $@"

{stringUsings}
namespace {microsoftServiceRegistration.Namespace};

public static class {microsoftServiceRegistration.ContainerName}_GeneratedServiceRegistration
{{
    public static void RegisterServices({microsoftServiceRegistration.ContainerType} builder)
    {{
        {servicesBlock}
    }}

    public static void AfterContainerBuilt({microsoftServiceRegistration.ProviderType} provider)
    {{
        {fireAfterContainerBuilt}
    }}
}}";

        return $"{template}";
    }
}