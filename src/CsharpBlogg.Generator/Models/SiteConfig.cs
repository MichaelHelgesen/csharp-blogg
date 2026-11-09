namespace CsharpBlogg.Generator.Models;

public class SiteConfig
{
    public string ContentPath { get; set; } = "content/posts";
    public string OutputPath { get; set; } = "output";
    public string PagesPath { get; set; } = "content/pages";
    public string TemplatesPath { get; set; } = "templates";
}
