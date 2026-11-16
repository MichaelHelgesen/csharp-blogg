using CsharpBlogg.Generator.Models;
using CsharpBlogg.Generator.Services;

// Prøv å lese konfigurasjonsfilen
SiteConfig siteConfig;
try
{
    siteConfig = SiteConfig.Load();
}
catch (Exception e)
{
    Console.WriteLine($"Error loading site configuration: {e.Message}");
    return 1;
}

FolderSetup.RunFolderSetup(siteConfig); // Sjekk at mappene finnes og er tilgjengelige.

var posts = ContentReader.LoadContent<Post>(siteConfig.PostFolderPath);
var pages = ContentReader.LoadContent<Page>(siteConfig.PageFolderPath);

var siteContent = new SiteContent(siteConfig, posts, pages);

ContentConverter.WriteContentToHtml(siteContent);

return 0;

