namespace CsharpBlogg.Generator.Models;

public class Post : Content
{
    public List<string> Tags { get; set; } = new List<string>();
}