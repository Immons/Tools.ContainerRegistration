using Tools.ContainerRegistration.Attributes;

namespace Tools.ContainerRegistration.Sample;

[ServiceRegistration([typeof(TestService)])]
public class TestService
{
}