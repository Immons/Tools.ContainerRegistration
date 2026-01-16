using System.Collections.Generic;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Microsoft;

public class MicrosoftServiceRegistration : ServiceRegistration
{
    public List<string> FireAfterContainerBuilt { get; set; }

    public MicrosoftServiceRegistration()
    {
        // Add System namespace for IServiceProvider
        Usings.Add("System");
    }

    public override string Build(IGenerator generator)
    {
        return MicrosoftServiceRegistrationTemplate.GenerateServiceRegistration(generator as MicrosoftGenerator, this);
    }

    public override IEnumerable<GeneratedSourceFile> BuildSplit(IGenerator generator, Dictionary<string, List<ServiceRegistrationEntity>> groupedEntities)
    {
        return MicrosoftServiceRegistrationTemplate.GenerateSplitServiceRegistration(generator as MicrosoftGenerator, this, groupedEntities);
    }
}