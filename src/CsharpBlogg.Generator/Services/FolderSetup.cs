
using CsharpBlogg.Generator.Models;

namespace CsharpBlogg.Generator.Services
{
    internal class FolderSetup
    {
        internal static void runFolderSetup(SiteConfig siteConfig)
        {
            if (!Directory.Exists(siteConfig.ContentPath)) // Sjekk etter mappe med post og pages
            {
                Console.WriteLine($"Source folder ({siteConfig.ContentPath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
                return;
            }

            try
            {
                Directory.CreateDirectory(siteConfig.OutputPath); // Prøv å opprette katalog
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"No access to {siteConfig.OutputPath} - {e}");
                return;
            }
        } 
    }
}