using CsharpBlogg.Generator.Models;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using YamlDotNet.Serialization;

var markdownPipeline = new MarkdownPipelineBuilder()
    .UseYamlFrontMatter() // Hente ut YAML
    .Build(); // Bygge rørgaten.

var yamlDeserializer = new DeserializerBuilder()
    .WithCaseInsensitivePropertyMatching() // Matche små og store bokstaver
    .IgnoreUnmatchedProperties() // Ignorere manglende felter i klassen som matches mot
    .Build();

string outputPath = "output";
string sourcePath = "content/posts";

if (!Directory.Exists(sourcePath))
{
    Console.WriteLine($"Source folder ({sourcePath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
    return;
}


// Filene å gå gjennom. EnumerationOptions er "hvordan skal filene gjennomgås".
// Her så skal alle filer matche store og små bokstaver i "md".
var files = GetFiles(sourcePath, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

try
{
    // Prøv å opprette katalog
    Directory.CreateDirectory(outputPath);
}
catch (UnauthorizedAccessException e)
{
    Console.WriteLine($"No access to {outputPath} - {e}");
    return;
}

// Iterere over filer
foreach (var file in files)
{
    try
    {
        var fileAsString = File.ReadAllText(file.FullName); // Leser filen
        var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.
        var html = Markdown.ToHtml(markdownTree, markdownPipeline); // Til HTML

        var post = new Post();

        var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.

        if (yamlBlock != null)
        {
            var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
            post = yamlDeserializer.Deserialize<Post>(yaml) ?? post; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
        }

        post.Html = html;

        // Skriv til HTML
        File.WriteAllText(Path.Combine(outputPath, Path.ChangeExtension(file.Name, "html")), html);
        
        Console.WriteLine($"File {file.Name} generated");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting and creating file {file.FullName} - {ex}");
    }
}

FileInfo[] GetFiles(string path, string pattern, EnumerationOptions options)
{
    return new DirectoryInfo(path).GetFiles(pattern, options);
}