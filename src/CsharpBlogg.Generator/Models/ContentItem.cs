namespace CsharpBlogg.Generator.Models;

public class ContentItem
{
    public string? Title { get; set; } 
    public string? MetaTitle { get; set; } 
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public DateTime? Published { get; set; }
    public DateTime? Updated { get; set; }
    public DateTime? Created { get; set; }
    public bool Draft { get; set; } = true;
    public string? Html { get; set; }
}