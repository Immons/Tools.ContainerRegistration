namespace Tools.ContainerRegistration.Common.Models;

public class IocConfig
{
    public string InterfaceNamingConvention { get; set; }
    public string[] ExcludedFromRegisteringAsConventionInterface { get; set; }
    public bool RegisterAsSelf { get; set; }
    public string[] RegisterTypesEndingWith { get; set; }
}