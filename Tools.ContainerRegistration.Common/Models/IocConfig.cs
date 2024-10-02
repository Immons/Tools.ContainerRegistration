namespace Tools.ContainerRegistration.Common.Models;

public class IocConfig
{
    public string[] ExcludedFromRegisteringMatching { get; set; }
    public string[] RegisterTypesMatching { get; set; }
    public string[] RegisterInterfacesOnlyFromThatAssemblies { get; set; }
    public bool RegisterAsSelf { get; set; }
    public bool RegisterAsAllInheritedTypes { get; set; }
    public bool RegisterAsDirectlyInheritedTypes { get; set; }
}