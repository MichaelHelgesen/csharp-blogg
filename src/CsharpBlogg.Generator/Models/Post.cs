namespace CsharpBlogg.Generator.Models;

public class Post
{
    public string Title { get; set; } 
    public string MetaTitle { get; set; } 
    public string Description { get; set; }
    public string Slug { get; set; }
    public DateTimeOffset? Published { get; set; } = null;
    public DateTimeOffset? Updated { get; set; } = null;
    public List<string> Tags { get; set; } = new List<string>();
    public bool Draft { get; set; } = true;
    public string Html { get; set; } = "";
}