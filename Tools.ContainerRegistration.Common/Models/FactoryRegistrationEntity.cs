namespace Tools.ContainerRegistration.Common.Models;

public class FactoryRegistrationEntity
{
    public FactoryRegistrationEntity(string factoryMethodName, bool scoped = false)
    {
        FactoryMethodName = factoryMethodName;
        Scoped = scoped;
    }

    public string FactoryMethodName { get; }

    /// <summary>
    /// If true, registers as Scoped instead of the default scope.
    /// </summary>
    public bool Scoped { get; }
}