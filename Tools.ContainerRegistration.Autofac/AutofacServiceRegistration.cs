using System.Collections.Generic;
using Tools.ContainerRegistration.Common.Generators.Interfaces;
using Tools.ContainerRegistration.Common.Models;

namespace Tools.ContainerRegistration.Autofac;

public class AutofacServiceRegistration : ServiceRegistration
{
    public override string Build(IGenerator generator) =>
        AutofacServiceRegistrationTemplate.GenerateServiceRegistration(generator, this);

    public override IEnumerable<GeneratedSourceFile> BuildSplit(IGenerator generator, Dictionary<string, List<ServiceRegistrationEntity>> groupedEntities) =>
        AutofacServiceRegistrationTemplate.GenerateSplitServiceRegistration(generator, this, groupedEntities);
}