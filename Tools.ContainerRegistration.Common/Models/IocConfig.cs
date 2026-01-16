namespace Tools.ContainerRegistration.Common.Models;

public class IocConfig
{
    public string[]? ExcludedFromRegisteringMatching { get; set; }
    public string[]? RegisterTypesMatching { get; set; }
    public string[]? RegisterInterfacesOnlyFromThatAssemblies { get; set; }
    public string[]? ScanAssemblies { get; set; }
    public bool? RegisterAsSelf { get; set; }
    public bool? RegisterAsAllInheritedTypes { get; set; }
    public bool? RegisterAsDirectlyInheritedTypes { get; set; }

    /// <summary>
    /// When true, splits generated registration into multiple smaller files grouped by type suffix.
    /// Each group gets its own partial class file with a Register{Group} method.
    /// A main file aggregates all group methods into RegisterServices.
    /// Default: false (single file generation)
    /// </summary>
    public bool? SplitGeneratedFiles { get; set; }
}