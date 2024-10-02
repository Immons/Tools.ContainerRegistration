namespace Tools.ContainerRegistration.Common.Models;

public class FactoryRegistrationEntity
{
    public FactoryRegistrationEntity(string factoryMethodName)
    {
        FactoryMethodName = factoryMethodName;
    }

    public string FactoryMethodName { get; }
}