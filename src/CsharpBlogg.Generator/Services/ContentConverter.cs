using CsharpBlogg.Generator.Models;
using Markdig;
using Markdown.ColorCode;
using Scriban;
using Scriban.Runtime;

namespace CsharpBlogg.Generator.Services
{
    internal static class ContentConverter
    {
        internal static void WriteContentToHtml(SiteContent siteContent)
        {

            foreach (var contentItem in siteContent.PublishedSlugs)
            {
                Console.WriteLine($"Converting {contentItem.Filename} to HTML...");
                /*
                if (contentItem.Published == null || contentItem.Published > DateTime.Now || string.IsNullOrEmpty(contentItem.Slug) || contentItem.Draft)
                {
                    Console.WriteLine($"Error: Content item {contentItem.Filename} is not published or is a draft or has an empty slug. Skipping this file.");
                    continue; // Hvis HTML-innholdet er tomt, hopp over denne filen.
                }
                */

                //
                // TODO: Flytt markdownPipeline til globalt sted, slik at det ikke må gjøres for hver fil?
                //
                var markdownPipeline = new MarkdownPipelineBuilder()
                    .UseYamlFrontMatter() // Hente ut YAML. Trengs ikke her?
                    //.UseAdvancedExtensions()
                    .UseColorCode()
                    .Build(); // Bygge rørgaten.


                var html = string.Empty;

                //string contentType = this.GetType().Name.ToLowerInvariant(); // Henter navnet på klassen som arver fra ContentItem, f.eks. "post" eller "page".

                // Definer template for innholdet, enten fra YAML eller default.
                if (string.IsNullOrEmpty(contentItem.Template)) // Hvis YAML er tomt
                {
                    var pathcheck = File.Exists(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), contentItem.Filename + ".html"));
                    //Console.WriteLine($"Defining template for {contentItem.Filename} in path: {file} -- {Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), contentItem.Filename + ".html")}");
                    if (pathcheck) // Hvis template finnes i templates/<contentType>/<filename>.html, bruk den.
                    {
                        //var file = Directory.EnumerateFiles(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant()), contentItem.Filename + ".html").FirstOrDefault();

                        contentItem.Template = Path.Combine(contentItem.GetType().Name.ToLowerInvariant(), contentItem.Filename + ".html");
                    }
                    else // Hvis ikke, sjekk om default.html finnes i templates/<contentType>/default.html
                    {
                        var pathCheck = File.Exists(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), "default.html"));
                        //Console.WriteLine($"Defining template for {contentItem.Filename} in path: {file2} -- {Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), "default.html")}");
                        if (pathCheck) // Hvis default.html finnes i templates/<contentType>/default.html, bruk den.
                        {
                           // var file2 = Directory.EnumerateFiles(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant()), "default.html").FirstOrDefault();

                            contentItem.Template = Path.Combine(contentItem.GetType().Name.ToLowerInvariant(), "default.html");
                        }
                        else // Hvis ikke, bruk templates/general/default.html
                        {
                            //contentItem.Template = Path.Combine("general/default.html");
                        }
                    }
                }
                else // Hvis YAML har definert en template, sjekk om den finnes i templates/<contentType>/<template>.html
                {
                    var file3 = Directory.EnumerateFiles(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant()), contentItem.Template + ".html").FirstOrDefault();
                    Console.WriteLine($"Defining template for {contentItem.Filename} in path: {file3} -- {Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), contentItem.Template + ".html")}");
                    if (file3 != null) // Hvis template finnes i templates/<contentType>/<template>.html, bruk den.
                    {
                        contentItem.Template = Path.Combine(contentItem.GetType().Name.ToLowerInvariant(), contentItem.Template + ".html");
                    }
                    else // Hvis ikke, bruk templates/general/default.html
                    {
                        //contentItem.Template = Path.Combine("general/default.html");
                    }
                }
               

                    // 1. Opprett en instans av loaderen med rotmappen for malene dine
                    var templateLoader = new MyIncludeFromDisk(siteContent.SiteConfig.TemplatesPath);

                    // 2. Opprett en TemplateContext og fest loaderen til den
                    var context = new TemplateContext
                    {
                        TemplateLoader = templateLoader
                    };


                contentItem.Html = Markdig.Markdown.ToHtml(contentItem.MarkdownBody ?? string.Empty, markdownPipeline); // Konverterer markdown til HTML

                // 3. Legg til globale variabler som kan brukes i malene
                var scriptObject = new ScriptObject
                {
                    { "basepath", siteContent.SiteConfig.BasePath }, // Bane for index.
                    { "main", contentItem.Html }, // Innhold fra markdown-filen
                    { "title", siteContent.SiteConfig.Title }, // Tittel for innholdet
                    { "sitecontent", siteContent }
                };

                context.PushGlobal(scriptObject);

                var baseTemplate = new DirectoryInfo(siteContent.SiteConfig.TemplatesPath)
                        .GetFiles("general/default.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

                var customTemplate = new DirectoryInfo(siteContent.SiteConfig.TemplatesPath)
                        .GetFiles(contentItem.Template, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

                
                if (customTemplate != null)
                {

                    string customTemplateContent = "";
                    customTemplateContent = File.ReadAllText(customTemplate.FullName);
                    var parsedCustomTemplate = Template.Parse(customTemplateContent); // Parse innholdet til en Scriban-mal
                    scriptObject["main"] = parsedCustomTemplate.Render(context); // Render malen med konteksten som inneholder variablene
                }


                //content = File.ReadAllText(baseTemplate.FullName); // Les innholdet i malen, eller default.html hvis malen ikke finnes.
                string content = File.ReadAllText(baseTemplate.FullName);
                var parsedTemplate = Template.Parse(content); // Parse innholdet til en Scriban-mal
                html += parsedTemplate.Render(context); // Render malen med konteksten som inneholder variablene


                    //html += contentItem.Html; // Innhold fra markdown-filen
                    contentItem.Html = html;
                    //Console.WriteLine(contentItem.Html);
                    
                    SaveFile.SaveFilesToDisk(siteContent.SiteConfig, contentItem);
                }

            }
        }
}


