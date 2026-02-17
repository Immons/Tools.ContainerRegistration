namespace Tools.ContainerRegistration.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class FactoryRegistrationAttribute : Attribute
{
    public FactoryRegistrationAttribute(string factoryFullPath, bool scoped = false)
    {
        FactoryFullPath = factoryFullPath;
        Scoped = scoped;
    }

    public string FactoryFullPath { get; }

    /// <summary>
    /// If true, registers as Scoped instead of Singleton.
    /// Use this when the factory resolves scoped dependencies.
    /// </summary>
    public bool Scoped { get; }
}