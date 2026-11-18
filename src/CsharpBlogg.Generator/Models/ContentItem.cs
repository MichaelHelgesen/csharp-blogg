using CsharpBlogg.Generator.Utilities;
using Scriban.Syntax;
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
    public bool Draft { get; set; } = false;
    public string? Html { get; set; } = string.Empty;
    public string? MarkdownBody { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;   
    public string Template { get; set; } = string.Empty;

    public virtual void Normalize() // Virtuel metode: kan overstyres i avledede klasser.
    {
        
    }

    public void SetSlug()
    {
        if (string.IsNullOrEmpty(Slug))
        {
            string filenameAsSlug = SlugGenerator.Slug(Filename);
            Console.WriteLine($"Slug not specified in YAML for {Filename}. Using filename as slug: {filenameAsSlug}");
            Slug = filenameAsSlug;
        }
        else
        {
            Slug = SlugGenerator.Slug(Slug); // Kjør SlugConverter på slug som er angitt i YAML, for å sikre at den er gyldig.
        }
    }
}