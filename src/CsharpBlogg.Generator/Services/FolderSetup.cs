
using CsharpBlogg.Generator.Models;

namespace CsharpBlogg.Generator.Services
{
    internal static class FolderSetup
    {
        internal static void RunFolderSetup(SiteConfig siteConfig)
        {
            if (!Directory.Exists(siteConfig.ContentPath)) // Sjekk etter mappe med post og pages
            {
                Console.WriteLine($"Source folder ({siteConfig.ContentPath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
                return;
            }

            if (!Directory.Exists(siteConfig.ResourceFolder)) // Sjekk etter mappefor ressurser
            {
                Console.WriteLine($"Source folder ({siteConfig.ResourceFolder}) is missing or not accessible.");
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
        public static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true)
        {
            if (!Directory.Exists(sourceDir)) return;

            // 1. Sørg for at målmappen finnes
            Directory.CreateDirectory(destinationDir);

            // 2. Finn alle filer (inkludert i undermapper) og kopier dem
            foreach (string sourceFilePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                // Finn den relative stien (f.eks. "css/style.css")
                string relativePath = Path.GetRelativePath(sourceDir, sourceFilePath);
                string destFilePath = Path.Combine(destinationDir, relativePath);

                // Opprett undermappen i output hvis den ikke finnes (f.eks. "output/assets/css")
                string? destSubDir = Path.GetDirectoryName(destFilePath);
                if (!string.IsNullOrEmpty(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                }

                // Kopier filen og overskriv hvis den finnes fra før
                File.Copy(sourceFilePath, destFilePath, overwrite);
            }
        }
    }
}