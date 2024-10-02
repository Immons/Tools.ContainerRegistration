using Microsoft.CodeAnalysis;
using Tools.ContainerRegistration.Common;
using Tools.ContainerRegistration.Common.Generators;
using Tools.ContainerRegistration.Common.Generators.Interfaces;

namespace Tools.ContainerRegistration.Autofac;

[Generator]
public class AutofacServiceRegistrationGenerator : ServiceRegistrationGenerator
{
    protected override IGenerator Generator => new AutofacGenerator();
}