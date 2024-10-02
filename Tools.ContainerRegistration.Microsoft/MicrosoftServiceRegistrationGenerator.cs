using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Common;
using Tools.ContainerRegistration.Common.Generators;
using Tools.ContainerRegistration.Common.Generators.Interfaces;

namespace Tools.ContainerRegistration.Microsoft;

[Generator]
public class MicrosoftServiceRegistrationGenerator : ServiceRegistrationGenerator
{
    protected override IGenerator Generator => new MicrosoftGenerator();
}