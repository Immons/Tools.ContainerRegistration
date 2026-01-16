namespace Tools.ContainerRegistration.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class FactoryRegistrationAttribute : Attribute
{
    public FactoryRegistrationAttribute(string factoryFullPath)
    {
        FactoryFullPath = factoryFullPath;
    }

    public string FactoryFullPath { get; }
}