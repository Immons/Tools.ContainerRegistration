namespace Tools.ContainerRegistration.Attributes;

/// <summary>
/// Marks a class that requires dependency injection via IInjector&lt;T&gt; pattern.
/// The source generator will generate OnActivating code to inject dependencies.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class InjectDependenciesAttribute : Attribute
{
}
