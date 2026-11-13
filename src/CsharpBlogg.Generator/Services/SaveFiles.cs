using CsharpBlogg.Generator.Models;

namespace CsharpBlogg.Generator.Services
{
    internal class SaveFiles
    {
        internal static void SaveFilesToDisk<T>(SiteConfig siteConfig, T contentItem) where T : ContentItem
        {
            // Skriv til HTML
            File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(contentItem.Slug ?? contentItem.Filename, "html")), contentItem.Html);
            Console.WriteLine($"File {contentItem.Title} generated, with slug: {contentItem.Slug}");
        }
    }
}
