using System.Collections.Generic;
using Tools.ContainerRegistration.Common.Generators.Interfaces;

namespace Tools.ContainerRegistration.Common.Models;

/// <summary>
/// Represents a single generated source file output.
/// </summary>
public class GeneratedSourceFile
{
    public string HintName { get; set; }
    public string Content { get; set; }
}

public abstract class ServiceRegistration
{
    public abstract string Build(IGenerator generator);

    /// <summary>
    /// Builds multiple source files when SplitGeneratedFiles is enabled.
    /// Returns a list of (hintName, content) pairs for each generated file.
    /// </summary>
    public abstract IEnumerable<GeneratedSourceFile> BuildSplit(IGenerator generator, Dictionary<string, List<ServiceRegistrationEntity>> groupedEntities);

    public string ContainerName { get; set; }
    public string ContainerType { get; set; }
    public string ProviderType { get; set; }
    public string Namespace { get; set; }
    public List<string> Usings { get; set; } = new List<string>();
    public List<ServiceRegistrationEntity> Entities { get; set; } = new List<ServiceRegistrationEntity>();
}