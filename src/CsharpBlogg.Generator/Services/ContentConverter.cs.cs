using CsharpBlogg.Generator.Models;
using Markdig;
using Scriban;
using Scriban.Runtime;

namespace CsharpBlogg.Generator.Services
{
    internal static class ContentConverter
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

                // 3. Legg til globale variabler som kan brukes i malene
                var scriptObject = new ScriptObject
                {
                    { "basepath", siteConfig.BasePath }, // Bane for å finne maler definert med include.
                    { "content", contentItem.Html } // Innhold fra markdown-filen
                };

                context.PushGlobal(scriptObject);

                // Finn og les default.html fra templates/general
                var defaultTemplate = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/default.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
                if (defaultTemplate != null)
                {
                    string content = File.ReadAllText(defaultTemplate.FullName); // Les innholdet i malen
                    var template = Template.Parse(content); // Parse innholdet til en Scriban-mal
                    html += template.Render(context); // Render malen med konteksten som inneholder variablene
                }
                
                if (contentItem.GetType().Name == "Page")
                {
                    var posts = ContentReader.LoadContent<Post>(siteConfig.PostFolderPath);
                    if (posts != null && posts.Count > 0)
                    {
                        var index = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("index.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
                        string content2 = File.ReadAllText(index.FullName);
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
                
                SaveFiles.SaveFilesToDisk(siteConfig, contentItem);
                }
            }
        }
}
