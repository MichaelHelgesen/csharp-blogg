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
FolderSetup.CopyDirectory(siteConfig.ResourceFolder, Path.Combine(siteConfig.OutputPath, siteConfig.ResourceFolder), overwrite: true); // Kopier ressurser til output-mappen

var posts = ContentReader.LoadContent<Post>(siteConfig.PostFolderPath);
var pages = ContentReader.LoadContent<Page>(siteConfig.PageFolderPath);

var siteContent = new SiteContent(siteConfig, posts, pages);

foreach (var item in siteContent.AllTags)
{
    Console.WriteLine(item);
}

ContentConverter.WriteContentToHtml(siteContent);



return 0;

