namespace Tools.ContainerRegistration.Attributes;

public class ServiceRegistrationAttribute : Attribute
{
    public ServiceRegistrationAttribute(Type[] registerAs)
    {
        RegisterAs = registerAs;
    }
    
    public Type[] RegisterAs { get; }
}