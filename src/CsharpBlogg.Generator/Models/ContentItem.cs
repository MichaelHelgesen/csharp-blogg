namespace CsharpBlogg.Generator.Models;

public abstract class ContentItem // Abstrakt: kan ikke instansieres direkte, men kan brukes som baseklasse for andre klasser.
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
    public string Filename { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;

    public virtual void Normalize() // Virtuel metode: kan overstyres i avledede klasser.
    {
        
    }
}