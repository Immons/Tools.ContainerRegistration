using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Tools.ContainerRegistration.Tests.TestHelpers;

public abstract class GeneratorTestBase
{
    protected abstract IIncrementalGenerator CreateGenerator();

    protected string RunGenerator(string sourceCode, string? configJson = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Tools.ContainerRegistration.Attributes.SingletonAttribute).Assembly.Location)
        };

        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var systemRuntimePath = Path.Combine(runtimePath, "System.Runtime.dll");
        if (File.Exists(systemRuntimePath))
            references.Add(MetadataReference.CreateFromFile(systemRuntimePath));

        var netstandardPath = Path.Combine(runtimePath, "netstandard.dll");
        if (File.Exists(netstandardPath))
            references.Add(MetadataReference.CreateFromFile(netstandardPath));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = CreateGenerator();

        var additionalTexts = configJson != null
            ? ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("ioc_config.json", configJson))
            : ImmutableArray<AdditionalText>.Empty;

        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(additionalTexts);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("GeneratedServiceRegistration"))
            .ToList();

        return generatedTrees.Any()
            ? generatedTrees.First().GetText().ToString()
            : string.Empty;
    }

    protected class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            _text = text;
        }

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default)
            => SourceText.From(_text);
    }
}
