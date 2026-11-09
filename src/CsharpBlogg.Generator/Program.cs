using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsharpBlogg.Generator.Models;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Scriban;
using YamlDotNet.Serialization;

var markdownPipeline = new MarkdownPipelineBuilder()
    .UseYamlFrontMatter() // Hente ut YAML
    .Build(); // Bygge rørgaten.

var yamlDeserializer = new DeserializerBuilder()
    .WithCaseInsensitivePropertyMatching() // Matche små og store bokstaver
    .IgnoreUnmatchedProperties() // Ignorere manglende felter i klassen som matches mot
    .Build();


// TODO: Se etter argument fra terminal
/* 
Console.WriteLine($"parameter count = {args.Length}");

for (int i = 0; i < args.Length; i++)
{
    Console.WriteLine($"Arg[{i}] = [{args[i]}]");
}
*/


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
List<Post> postList = new List<Post>();

// Iterere over filer
foreach (var file in files)
{
    try
    {
        var fileAsString = File.ReadAllText(file.FullName); // Leser filen

        var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.

        var post = new Post();

        var html = "";

        var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
        if (yamlBlock != null)
        {
            var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
            post = yamlDeserializer.Deserialize<Post>(yaml) ?? post; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
        }

        var header = GetFiles(siteConfig.TemplatesPath, "general/header.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
        string content = File.ReadAllText(header.FullName);
        var templateTEst = Template.Parse(content);

        html += templateTEst.Render();

        html += Markdown.ToHtml($"# {post.Title}", markdownPipeline); // Tittel fra YAML
        html += Markdown.ToHtml(markdownTree, markdownPipeline); // Tekst til HTML

        post.Tags = post.Tags.Select(tag => tag.ToLowerInvariant()).ToList(); // Gjør alle tags til små bokstaver

        post.Created = file.CreationTimeUtc;

        if (post.Slug == null || post.Slug == "")
        {
            post.Slug = slugConverter(file.Name.Split(".")[0]);
        }
        else
        {
            post.Slug = slugConverter(post.Slug);
        }

        if (post.MetaTitle == null || post.MetaTitle == "")
        {
            post.MetaTitle = post.Title;
        }

        post.Html = html;

        

        postList.Add(post);

        // Skriv til HTML
        File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(post.Slug??file.Name, "html")), post.Html);

        //Console.WriteLine($"File {file.Name} generated, with slug: {post.Slug}");
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Error genereating {file.Name} in {siteConfig.OutputPath}: {ex.Message}");
        throw;
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"No access to {siteConfig.OutputPath}: {ex.Message}");
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting and creating file {file.FullName} - {ex}");
    }
}

// Maler
var templates = GetFiles(siteConfig.TemplatesPath, "*.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

// Sider
if (!Directory.Exists(siteConfig.PagesPath))
{
    Console.WriteLine($"Pages folder ({siteConfig.ContentPath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
    return;
}

var pages = GetFiles(siteConfig.PagesPath, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

foreach (var item in pages)
{
    Console.WriteLine(item.Name);
    string pageFileName = item.Name.Split(".")[0];
    var fileAsString = File.ReadAllText(item.FullName); // Leser filen


    var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.

    var post = new Page();


    var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
    if (yamlBlock != null)
    {
        var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
        post = yamlDeserializer.Deserialize<Page>(yaml) ?? post; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
    }

    var html = Markdown.ToHtml($"# {post.Title}", markdownPipeline); // Tittel fra YAML
    html += Markdown.ToHtml(markdownTree, markdownPipeline); // Tekst til HTML

    if (post.Slug == null || post.Slug == "")
    {
        post.Slug = slugConverter(item.Name.Split(".")[0]);
    }
    else
    {
        post.Slug = slugConverter(post.Slug);
    }

    if (post.MetaTitle == null || post.MetaTitle == "")
    {
        post.MetaTitle = post.Title;
    }

    post.Html = html;

    // SEtte inn template
    foreach (var template in templates)
    {
        if (template.Name.Split(".")[0] == pageFileName)
        {
            string content = File.ReadAllText(template.FullName);
            var templateTEst = Template.Parse(content);
            var result = templateTEst.Render(new { Name = "World", posts = postList });

            post.Html += result;
        }
    }

    // Skriv til HTML
    File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(post.Slug, "html")), post.Html);

    //Console.WriteLine($"Page {item.Name} generated, with slug: {post.Slug}");




}

static FileInfo[] GetFiles(string path, string pattern, EnumerationOptions options)
{
    return new DirectoryInfo(path).GetFiles(pattern, options);
}

static SiteConfig LoadConfig()
{
    var jsonConfigFilePath = "siteconfig.json"; // Banen til konfigurasjonsfilen
    var siteConfig = new SiteConfig();
    if (!Directory.Exists(siteConfig.ContentPath))
    {
        Console.WriteLine($"Source folder ({siteConfig.ContentPath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
        return siteConfig;
    }
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

static string slugConverter(string title)
{
    string titleCopy = title
            .ToLowerInvariant()
            // .Trim()
            .Replace("ø", "o").Replace("å", "aa").Replace("æ", "ae");

    titleCopy = titleCopy.Normalize(NormalizationForm.FormD);
    char[] arr = titleCopy.ToCharArray();

    arr = Array.FindAll(arr, c => char.IsAsciiLetterOrDigit(c)
                                      /* || char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark - feil*/
                                      || char.IsWhiteSpace(c)
                                      || c == '-');
    titleCopy = new string(arr);
    titleCopy = Regex.Replace(titleCopy, @"[\s-]+", "-");
    //test = Regex.Replace(test, @"[-]+", "-");
    titleCopy = titleCopy.Trim('-'); // kan ikke bruke ", for det er ikke char.
                                     //titleCopy = titleCopy.TrimStart('-'); // Trim tar begge ender, trenger ikke start og end
    titleCopy = titleCopy + ".html";
    return titleCopy;
}