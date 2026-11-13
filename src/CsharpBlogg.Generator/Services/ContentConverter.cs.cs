using CsharpBlogg.Generator.Models;
using Markdig;
using Scriban;
using Scriban.Runtime;

namespace CsharpBlogg.Generator.Services
{
    internal class ContentConverter
    {
        internal static void WriteContentToHtml<T>(SiteConfig siteConfig, List<T> contentItems) where T : ContentItem
        {

            foreach (var contentItem in contentItems)
            {
                //
                // TODO: Flytt markdownPipeline til globalt sted, slik at det ikke må gjøres for hver fil?
                //
                var markdownPipeline = new MarkdownPipelineBuilder()
                    .UseYamlFrontMatter() // Hente ut YAML. Trengs ikke her?
                    .Build(); // Bygge rørgaten.

                var type = contentItem.GetType().Name; // Hvilken type innhold er det? Page eller Post?

                contentItem.Normalize(); // Gjør alle tags til små bokstaver.

                var html = "";

                // 1. Opprett en instans av loaderen med rotmappen for malene dine
                var templateLoader = new MyIncludeFromDisk(siteConfig.TemplatesPath);

                // 2. Opprett en TemplateContext og fest loaderen til den
                var context = new TemplateContext
                {
                    TemplateLoader = templateLoader
                };

                var scriptObject = new ScriptObject
                {
                    { "basepath", siteConfig.BasePath },
                    { "content", contentItem.Html }
                };
                context.PushGlobal(scriptObject);

                // Finn og les default.html fra templates/general
                var defaultTemplate = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/default.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
                //var headerTemplate = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/header.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
                //var footerTemplate = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/footer.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
                if (defaultTemplate != null)
                {
                    /*
                    string footer = File.ReadAllText(footerTemplate.FullName);
                    var footerTemplateParsed = Template.Parse(footer);
                    var footerRendered = footerTemplateParsed.Render(new { basepath = siteConfig.BasePath });   

                    string header = File.ReadAllText(headerTemplate.FullName);
                    var headerTemplateParsed = Template.Parse(header);
                    var headerRendered = headerTemplateParsed.Render(new { basepath = siteConfig.BasePath });
                    */
                    string content = File.ReadAllText(defaultTemplate.FullName);
                    var template = Template.Parse(content);
                    html += template.Render(context);
                }
                
                //html += Markdown.ToHtml($"# {contentItem.Title}", markdownPipeline); // Tittel fra YAML

                if (contentItem.GetType().Name == "Page")
                {
                    var posts = ContentReader.LoadContent<Post>(siteConfig.PostFolderPath);
                    if (posts != null && posts.Count > 0)
                    {
                        var index = Directory.EnumerateFiles(
                        siteConfig.TemplatesPath,
                        "index.html",
                        new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }
                    ).FirstOrDefault();
                        string content2 = File.ReadAllText(index);
                        var templateTEst2 = Template.Parse(content2);
                        var result = templateTEst2.Render(new { posts });
                        html += result;
                    }
                    else
                    {
                        Console.WriteLine($"No posts found to list on page: {contentItem.Title}");

                    }
                     
                }
                //html += contentItem.Html; // Innhold fra markdown-filen
                contentItem.Html = html;
                // Skriv til HTML
                File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(contentItem.Slug ?? contentItem.Filename, "html")), contentItem.Html);
                Console.WriteLine($"File {contentItem.Title} generated, with slug: {contentItem.Slug}");
                }
            }
        }
}
