using CsharpBlogg.Generator.Models;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Scriban;
using YamlDotNet.Serialization;
using CsharpBlogg.Generator.Utilities;
using Scriban.Syntax;


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
    siteConfig = SiteConfig.Load();
}
catch // Beskjed kastes fra metoden.
{
    return;
}

// Sjekk etter mappe med post og pages
if (!Directory.Exists(siteConfig.ContentPath))
{
    Console.WriteLine($"Source folder ({siteConfig.ContentPath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
    return;
}


// Filene å gå gjennom. EnumerationOptions er "hvordan skal filene gjennomgås".
// Her så skal alle filer matche store og små bokstaver i "md".
///var posts = GetFiles(siteConfig.ContentPath + siteConfig.PostFolder, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });
//var pages = GetFiles(siteConfig.ContentPath + siteConfig.PageFolder, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive});
//var contentFiles = GetFiles(siteConfig.ContentPath, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true });
/*
    Console.WriteLine(contentFiles.Length);
    if (contentFiles.Length == 0)
    {
        Console.WriteLine($"No markdown files found in source folder: {siteConfig.ContentPath}");
        return;
    }
*/
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

var posts = LoadContent<Post>(siteConfig, siteConfig.PostFolder);
var pages = LoadContent<Page>(siteConfig, siteConfig.PageFolder);

foreach (var contentItem in posts)
{
    
    contentItem.Normalize(); // Normaliserer innholdet, f.eks. gjør alle tags til små bokstaver.

    var markdownPipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter() // Hente ut YAML
        .Build(); // Bygge rørgaten.

    var header = GetFiles(siteConfig.TemplatesPath, "general/header.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
    string content = File.ReadAllText(header.FullName);
    var templateTEst = Template.Parse(content);

    var html = "";

    html += templateTEst.Render(new { basepath = siteConfig.BasePath });

    html += Markdown.ToHtml($"# {contentItem.Title}", markdownPipeline); // Tittel fra YAML

            var type = contentItem.GetType().Name;

    html += contentItem.Html;
    contentItem.Html = html;
    // Skriv til HTML
    //File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(contentItem.Slug?? file.Name, "html")), contentItem.Html);
    File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(contentItem.Slug, "html")), contentItem.Html);
    Console.WriteLine($"File {contentItem.Title} generated, with slug: {contentItem.Slug}");
}

foreach (var contentItem in pages)
{
    var markdownPipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter() // Hente ut YAML
        .Build(); // Bygge rørgaten.

    var header = GetFiles(siteConfig.TemplatesPath, "general/header.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
    string content = File.ReadAllText(header.FullName);
    var templateTEst = Template.Parse(content);

    var html = "";

    html += templateTEst.Render(new { basepath = siteConfig.BasePath });

    html += Markdown.ToHtml($"# {contentItem.Title}", markdownPipeline); // Tittel fra YAML

            var type = contentItem.GetType().Name;


    if (type == "Page")
    Console.WriteLine(type);
    {
        var index = Directory.EnumerateFiles(
            siteConfig.TemplatesPath,
            "index.html",
            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }
        ).FirstOrDefault();
        string content2 = File.ReadAllText(index);
        var templateTEst2 = Template.Parse(content2);
        var result = templateTEst2.Render(new { Name = "World", posts = posts });
        html += result;
    }
    var lastContent = contentItem.Html;
    contentItem.Html = "";
    contentItem.Html += html;
    contentItem.Html += lastContent;
    // Skriv til HTML
    // TODO: Hvordan håndtere at slug kan være null? Skal vi bruke filnavnet i så fall? Men hvordan skal vi få tak i filnavnet her? Skal vi legge til en property for filnavn i ContentItem?
    //File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(contentItem.Slug?? file.Name, "html")), contentItem.Html);
    File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(contentItem.Slug, "html")), contentItem.Html);

    Console.WriteLine($"File {contentItem.Title} generated, with slug: {contentItem.Slug}");
}

/*
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
    string pageFileName = item.Name.Split(".")[0];
    var fileAsString = File.ReadAllText(item.FullName); // Leser filen


    var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.

    var page = new Page();


    var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
    if (yamlBlock != null)
    {
        var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
        page = yamlDeserializer.Deserialize<Page>(yaml) ?? page; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
    }

    var html = Markdown.ToHtml($"# {page.Title}", markdownPipeline); // Tittel fra YAML
    html += Markdown.ToHtml(markdownTree, markdownPipeline); // Tekst til HTML

    if (page.Slug == null || page.Slug == "")
    {
        page.Slug = Slug.SlugConverter(item.Name.Split(".")[0]);
    }
    else
    {
        page.Slug = Slug.SlugConverter(page.Slug);
    }

    if (page.MetaTitle == null || page.MetaTitle == "")
    {
        page.MetaTitle = page.Title;
    }

    page.Html = html;

    // SEtte inn template
    foreach (var template in templates)
    {
        if (template.Name.Split(".")[0] == pageFileName)
        {
            string content = File.ReadAllText(template.FullName);
            var templateTEst = Template.Parse(content);
            var result = templateTEst.Render(new { Name = "World", posts = postList });

            page.Html += result;
        }
    }

    // Skriv til HTML
    File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(page.Slug, "html")), page.Html);

    Console.WriteLine($"Page {item.Name} generated, with slug: {page.Slug}");




}*/

static FileInfo[] GetFiles(string path, string pattern, EnumerationOptions options)
{
    return new DirectoryInfo(path).GetFiles(pattern, options);
}


static List<T> LoadContent<T>(SiteConfig siteConfig, string contentFolder) where T : ContentItem, new()
{

    var markdownPipeline = new MarkdownPipelineBuilder()
    .UseYamlFrontMatter() // Hente ut YAML
    .Build(); // Bygge rørgaten.

    var yamlDeserializer = new DeserializerBuilder()
        .WithCaseInsensitivePropertyMatching() // Matche små og store bokstaver
        .IgnoreUnmatchedProperties() // Ignorere manglende felter i klassen som matches mot
        .Build();

    var files = GetFiles(siteConfig.ContentPath + "/" + contentFolder, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

    List<T> Content = [];

    // Iterere over filer
    foreach (var file in files)
    {
        try
        {

            var fileAsString = File.ReadAllText(file.FullName); // Leser filen

            var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.

            var html = "";

            var contentItem = new T();


            var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
            if (yamlBlock != null)
            {
                var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
                contentItem = yamlDeserializer.Deserialize<T>(yaml) ?? contentItem; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
            }

            html += Markdown.ToHtml(markdownTree, markdownPipeline); // Tekst til HTML
            contentItem.Created = file.CreationTimeUtc;

            if (contentItem.Slug == null || contentItem.Slug == "")
            {
                contentItem.Slug = Slug.SlugConverter(file.Name.Split(".")[0]);
            }
            else
            {
                contentItem.Slug = Slug.SlugConverter(contentItem.Slug);
            }

            if (contentItem.MetaTitle == null || contentItem.MetaTitle == "")
            {
                contentItem.MetaTitle = contentItem.Title;
            }
            
            contentItem.Html = html;

            Content.Add(contentItem);


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
    return Content;
}

