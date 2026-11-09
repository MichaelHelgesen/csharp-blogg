namespace CsharpBlogg.Generator.Models;

public class Post : ContentItem
{
    public List<string> Tags { get; set; } = new List<string>();
}