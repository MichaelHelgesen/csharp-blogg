using CsharpBlogg.Generator.Models;
using Markdig;
using Scriban;
using CsharpBlogg.Generator.Services;

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

FolderSetup.runFolderSetup(siteConfig); // Sjekk at mappene finnes og er tilgjengelige.

var posts = ContentReader.LoadContent<Post>(siteConfig, siteConfig.PostFolder);
var pages = ContentReader.LoadContent<Page>(siteConfig, siteConfig.PageFolder);

WriteContentToHtml(siteConfig, posts.Cast<ContentItem>().ToList());
WriteContentToHtml(siteConfig, pages.Cast<ContentItem>().ToList());

void WriteContentToHtml(SiteConfig siteConfig, List<ContentItem> contentItems)
{
    foreach (var contentItem in contentItems)
    {
        contentItem.Normalize(); // Normaliserer innholdet, f.eks. gjør alle tags til små bokstaver.

        var markdownPipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter() // Hente ut YAML
            .Build(); // Bygge rørgaten.

        var html = "";

        // Finn og les header.html fra templates/general
        var header = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/header.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
        if (header != null)
        {
            string content = File.ReadAllText(header.FullName);
            var template = Template.Parse(content);
            html += template.Render(new { basepath = siteConfig.BasePath });
        }

        html += Markdown.ToHtml($"# {contentItem.Title}", markdownPipeline); // Tittel fra YAML

        if (contentItem.GetType().Name == "Page")
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
        }//html += contentItem.Html; // Innhold fra markdown-filen
        contentItem.Html = html + contentItem.Html;

        // Skriv til HTML
        File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(contentItem.Slug ?? contentItem.Filename, "html")), contentItem.Html);
        //Console.WriteLine($"File {contentItem.Title} generated, with slug: {contentItem.Slug}");
    }
}
