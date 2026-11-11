
using CsharpBlogg.Generator.Models;
using CsharpBlogg.Generator.Utilities;
using Markdig;
using Markdig.Syntax;
using Markdig.Extensions.Yaml;
using YamlDotNet.Serialization;
namespace CsharpBlogg.Generator.Services
{
    internal class ContentReader
    {
        public static List<T> LoadContent<T>(SiteConfig siteConfig, string contentFolder) where T : ContentItem, new()
        {
            var markdownPipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter() // Hente ut YAML
                .Build(); // Bygge rørgaten.

            var yamlDeserializer = new DeserializerBuilder()
                .WithCaseInsensitivePropertyMatching() // Matche små og store bokstaver
                .IgnoreUnmatchedProperties() // Ignorere manglende felter i klassen som matches mot
                .Build();
            
            var files = new DirectoryInfo(siteConfig.ContentPath + "/" + contentFolder).GetFiles("*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });
            if (files.Length == 0)
            {
                Console.WriteLine($"No markdown files found in source folder: {siteConfig.ContentPath + "/" + contentFolder}");
                return new List<T>();
            }

            List<T> Content = []; // Liste med innhold som skal returneres

            foreach (var file in files)
            {
                try
                {
                    var fileAsString = File.ReadAllText(file.FullName); // Leser filen

                    var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.

                    var contentItem = new T();

                    Console.WriteLine($"{file.Name} read successfully. Parsing YAML and Markdown...{contentItem.Filename}");
                    var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
                    if (yamlBlock != null)
                    {
                        var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
                        contentItem = yamlDeserializer.Deserialize<T>(yaml) ?? contentItem; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
                    }

                    contentItem.Filename = file.Name.Split(".")[0];
                    contentItem.Created = file.CreationTimeUtc; // Markdown-filens opprettelsesdato settes som default for innholdets opprettelsesdato.

  
                    contentItem.Html = Markdown.ToHtml(markdownTree, markdownPipeline); // Markdown-tekst til HTML

                    if (contentItem.Slug == null || contentItem.Slug == "")
                    {
                        string filenameAsSlug = Slug.SlugConverter(contentItem.Filename);
                        Console.WriteLine($"Slug not specified in YAML for {contentItem.Filename}. Using filename as slug: {filenameAsSlug}");
                        if (filenameAsSlug == "" || filenameAsSlug == null)
                        {
                            Console.WriteLine($"Filename {contentItem.Filename} with title {contentItem.Title} could not be converted to a slug. Skipping this file.");
                            continue; // Hvis filnavnet ikke kan konverteres til en slug, hopp over denne filen.
                        }
                        contentItem.Slug = filenameAsSlug;
                    }
                    else
                    {
                        contentItem.Slug = Slug.SlugConverter(contentItem.Slug); // Kjør SlugConverter på slug som er angitt i YAML, for å sikre at den er gyldig.
                    }

                    if (contentItem.MetaTitle == null || contentItem.MetaTitle == "")
                    {
                        contentItem.MetaTitle = contentItem.Title;
                    }

                    Content.Add(contentItem);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Error reading {file.Name} in {siteConfig.OutputPath}: {ex.Message}");
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"No access to {siteConfig.OutputPath}: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading and parsing file {file.FullName} - {ex}");
                }

            }
            return Content;
        }
    }
}
