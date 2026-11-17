
using CsharpBlogg.Generator.Models;
using Markdig;
using Markdig.Syntax;
using Markdig.Extensions.Yaml;
using YamlDotNet.Serialization;
namespace CsharpBlogg.Generator.Services
{
    internal static class ContentReader
    {
        public static List<T> LoadContent<T>(string contentFolder) where T : ContentItem, new()
        {

            var markdownPipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter() // Hente ut YAML
                .Build(); // Bygge rørgaten.

            var yamlDeserializer = new DeserializerBuilder()
                .WithCaseInsensitivePropertyMatching() // Matche små og store bokstaver
                .IgnoreUnmatchedProperties() // Ignorere manglende felter i klassen som matches mot
                .Build();

            //string contentFolder = typeof(T).Name.ToLower() + "s"; // Hvilken type innhold er det? Page eller Post?

            var files = new DirectoryInfo(contentFolder).GetFiles("*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

            
            if (files.Length == 0)
            {
               // Console.WriteLine($"No markdown files found in source folder: {contentFolder}");
                return new List<T>();
            }

            List<T> content = []; // Liste med innhold som skal returneres

            foreach (var file in files)
            {
                try
                {
                    var fileAsString = File.ReadAllText(file.FullName); // Leser filen

                    var parentDirectory = Directory.GetParent(file.FullName);

                    var markdownTree = Markdig.Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.

                    var contentItem = new T();

                    var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
                    
                    if (yamlBlock != null)
                    {
                        var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
                        contentItem = yamlDeserializer.Deserialize<T>(yaml) ?? contentItem; // Instansierer et ContentItem-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
                        //contentItem.Markdown = (string?)fileAsString.Slice(yamlBlock.Span.End, fileAsString.Length - yamlBlock.Span.End); // Markdown-tekst til HTML
                        contentItem.MarkdownBody = fileAsString[(yamlBlock.Span.End +1)..]; // Markdown uten YAML.
                    }
                    else
                    {
                        contentItem.MarkdownBody = fileAsString; // Hvis ingen YAML, sett hele filen som Markdown.
                    }

                    contentItem.Filename = file.Name.Split(".")[0];
                    contentItem.Created = file.CreationTimeUtc; // TODO: Markdown-filens opprettelsesdato settes som default for innholdets opprettelsesdato.
                    
                    if (contentItem.MetaTitle == null || contentItem.MetaTitle == "")
                    {
                        contentItem.MetaTitle = contentItem.Title;
                    }

                    contentItem.SetSlug(); // Setter slug, enten fra YAML eller fra filnavnet.)

                    if (string.IsNullOrEmpty(contentItem.Slug))
                    {
                       // Console.WriteLine($"Filename {file.Name} with title {contentItem.Title} could not be converted to a slug. Skipping this file.");
                        continue; // Hvis filnavnet ikke kan konverteres til en slug, hopp over denne filen.
                    }

                    contentItem.FilePath = file.FullName; // Setter filbanen til innholdet.

                    //contentItem.DefineTemplate(parentDirectory); // Setter template, enten fra YAML eller default.

                    contentItem.Normalize(); // Normaliserer innholdet, f.eks. gjør alle tags til små bokstaver.

                    

                    content.Add(contentItem);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Error reading {file.Name}: {ex.Message}");
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"No access to {file.Name}: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading and parsing file {file.FullName} - {ex}");
                }

            }
            return content;
        }
    }
}
