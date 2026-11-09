namespace CsharpBlogg.Generator.Models;

public class ContentItem
{
    public string? Title { get; set; } 
    public string? MetaTitle { get; set; } 
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public DateTimeOffset? Published { get; set; }
    public DateTimeOffset? Updated { get; set; }
    public DateTimeOffset? Created { get; set; }
    public bool Draft { get; set; } = true;
    public string Html { get; set; } = "";
}