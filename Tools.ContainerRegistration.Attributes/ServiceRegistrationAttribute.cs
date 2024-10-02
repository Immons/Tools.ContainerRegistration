namespace Tools.ContainerRegistration.Attributes;

public class ServiceRegistrationAttribute : Attribute
{
    public ServiceRegistrationAttribute(Type[] interfaceTypes)
    {
        InterfaceTypes = interfaceTypes;
    }
    
    public Type[] InterfaceTypes { get; }
}