using System.Text.Json;

namespace CsharpBlogg.Generator.Models;

public class SiteConfig
{
    public string ContentPath { get; set; } = "content";
    public string OutputPath { get; set; } = "output";
    public string PostFolder { get; set; } = "posts";
    public string PageFolder { get; set; } = "pages";
    public string TemplatesPath { get; set; } = "templates";
    public string SiteUrl { get; set; } = string.Empty;
    public string BasePath { get; set; } = string.Empty;

    public static SiteConfig Load()
    {
        var jsonConfigFilePath = "siteconfig.json"; // Banen til konfigurasjonsfilen
        var siteConfig = new SiteConfig();
        if (!Directory.Exists(siteConfig.ContentPath))
        {
            Console.WriteLine($"Source folder ({siteConfig.ContentPath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
            return siteConfig;
        }
        if (File.Exists(jsonConfigFilePath))
        {
            try
            {
                var jsonContent = File.ReadAllText(jsonConfigFilePath);
                siteConfig = JsonSerializer.Deserialize<SiteConfig>(jsonContent) ?? siteConfig;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing {jsonConfigFilePath}: {ex.Message}");
                throw;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading {jsonConfigFilePath}: {ex.Message}");
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"No access to {jsonConfigFilePath}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error with {jsonConfigFilePath}: {ex.Message}");
                throw;
            }
        }
        else
        {
            try
            {
                Console.WriteLine($"Missing {jsonConfigFilePath}, attempting to create one with default values.");
                File.WriteAllText(jsonConfigFilePath, JsonSerializer.Serialize(siteConfig, new JsonSerializerOptions { WriteIndented = true })); // writeIndented = true som gir penere formatering.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not create {jsonConfigFilePath}: {ex.Message}");
            }
        }
        return siteConfig;
    }
}
