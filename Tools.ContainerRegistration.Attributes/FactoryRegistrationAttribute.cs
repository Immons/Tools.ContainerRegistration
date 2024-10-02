namespace Tools.ContainerRegistration.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class FactoryRegistrationAttribute : Attribute
{
    public FactoryRegistrationAttribute(string factoryMethodName)
    {
        FactoryMethodName = factoryMethodName;
    }

    public string FactoryMethodName { get; }
}