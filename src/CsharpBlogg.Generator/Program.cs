using System.Text.Json;
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

// Prøv å lese konfigurasjonsfilen
SiteConfig siteConfig;

try
{
    siteConfig = LoadConfig();
}
catch // Beskjed kastes fra metoden.
{
    return;
}


if (!Directory.Exists(siteConfig.ContentPath))
{
    Console.WriteLine($"Source folder ({siteConfig.ContentPath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
    return;
}


// Filene å gå gjennom. EnumerationOptions er "hvordan skal filene gjennomgås".
// Her så skal alle filer matche store og små bokstaver i "md".
var files = GetFiles(siteConfig.ContentPath, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

if (files.Length == 0)
{
    Console.WriteLine($"No markdown files found in source folder: {siteConfig.ContentPath}");
    return;
}

try
{
    // Prøv å opprette katalog
    Directory.CreateDirectory(siteConfig.OutputPath);
}
catch (UnauthorizedAccessException e)
{
    Console.WriteLine($"No access to {siteConfig.OutputPath} - {e}");
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
        File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(file.Name, "html")), post.Html);

        Console.WriteLine($"File {file.Name} generated");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting and creating file {file.FullName} - {ex}");
    }
}

static FileInfo[] GetFiles(string path, string pattern, EnumerationOptions options)
{
    return new DirectoryInfo(path).GetFiles(pattern, options);
}

static SiteConfig LoadConfig()
{
    var jsonConfigFilePath = "siteconfig.json"; // Banen til konfigurasjonsfilen
    var siteConfig = new SiteConfig();

    if (File.Exists(jsonConfigFilePath))
    {
        try
        {
            var jsonContent = File.ReadAllText(jsonConfigFilePath);
            siteConfig = JsonSerializer.Deserialize<SiteConfig>(jsonContent) ?? siteConfig;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing {jsonConfigFilePath}: {ex.Message}");
            throw;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading {jsonConfigFilePath}: {ex.Message}");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"No access to {jsonConfigFilePath}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error with {jsonConfigFilePath}: {ex.Message}");
            throw;
        }
    }
    else
    {
        try
        {
            Console.WriteLine($"Missing {jsonConfigFilePath}, attempting to create one with default values.");
            File.WriteAllText(jsonConfigFilePath, JsonSerializer.Serialize(siteConfig, new JsonSerializerOptions { WriteIndented = true })); // writeIndented = true som gir penere formatering.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not create {jsonConfigFilePath}: {ex.Message}");
        }
    }
    return siteConfig;
}