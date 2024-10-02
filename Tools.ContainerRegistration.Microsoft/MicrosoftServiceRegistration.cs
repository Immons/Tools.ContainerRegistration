using System.Collections.Generic;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Microsoft;

public class MicrosoftServiceRegistration : ServiceRegistration
{
    public List<string> FireAfterContainerBuilt { get; set; }

    public override string Build(IGenerator generator)
    {
        return MicrosoftServiceRegistrationTemplate.GenerateServiceRegistration(generator as MicrosoftGenerator, this);
    }
}