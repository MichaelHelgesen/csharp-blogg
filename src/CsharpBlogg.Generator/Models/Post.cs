namespace CsharpBlogg.Generator.Models;

public class Post : ContentItem
{
    public List<string> Tags { get; set; } = new List<string>();

    public override void Normalize()
    {
        base.Normalize();
        Tags??= [.. Tags.Select(tag => tag.ToLowerInvariant())]; // Gjør alle tags til små bokstaver
    }
}