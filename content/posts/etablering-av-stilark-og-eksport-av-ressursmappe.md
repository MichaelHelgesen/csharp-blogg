---
title: Etablering av stilark og eksport av ressursmappe
published: 2026-11-18
tags: [csharp, css, ssg-prosjekt, scriban]
---

**Inline-stiler blir fort veldig uoversiktlige og vanskelige å vedlikeholde. Derfor ville jeg heller etablere og ta i bruk et stilark.**

Jeg hadde allerede laget en mappe `resources` i rotmappen, og lagt inn en undermappe `css`. Der opprettet jeg en fil `style.css`, som jeg overførte alle inline-stilene til.

Jeg la også inn en lenke til stilarket i `default.html`:

```html
<head>
	<meta charset="UTF-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>{{ title }}</title>
	<link rel="stylesheet" href="resources/css/style.css" />
</head>
```

## Eksport av ressursmappe

For å få med ressursmappen i eksporten, måtte jeg lage en metode for å kopiere hele mappen til output-mappen. 

Det så ikke ut til at C# har noen mulighet for å kopiere en hel mappe, så jeg måtte lage en egen metode `CopyDirectory` for dette:

```csharp
public static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true)
{
    if (!Directory.Exists(sourceDir)) return;

    // Sjekk om målmappen finnes, og opprett den hvis den ikke gjør det
    Directory.CreateDirectory(destinationDir);

    // Iterer gjennom alle filene i kildemappen og kopierer dem til målmappen
    foreach (string sourceFilePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        // Finn den relative banen til filen i forhold til kildemappen
        string relativePath = Path.GetRelativePath(sourceDir, sourceFilePath);
        string destFilePath = Path.Combine(destinationDir, relativePath);

        // Opprett undermappen i output hvis den ikke finnes (f.eks. "output/resources/css")
        string? destSubDir = Path.GetDirectoryName(destFilePath);
        if (!string.IsNullOrEmpty(destSubDir))
        {
            Directory.CreateDirectory(destSubDir);
        }

        // Kopier filen og overskriv hvis den finnes fra før
        File.Copy(sourceFilePath, destFilePath, overwrite);
    }
}
```

Foreløpig la jeg denne metoden i klassen `FolderSetup`, og kjørte den fra `Program.cs` på følgende vis:

```csharp
FolderSetup.CopyDirectory(siteConfig.ResourceFolder, Path.Combine(siteConfig.OutputPath, siteConfig.ResourceFolder), overwrite: true); // Kopier ressurser til output-mappen
```

Jeg la også til `resoursefolder` i `SiteConfig`.

I stilarket `style.css` la jeg også inn en reset for å fjerne marginer og padding på alle elementer, og sette en standard font:

```css
/* RESET */
*, *::before, *::after {
  box-sizing: border-box;
}

* {
  margin: 0;
}

body {
  line-height: 1.6;
  -webkit-font-smoothing: antialiased;
}

img, picture, video, canvas, svg {
  display: block;
  max-width: 100%;
}

input, button, textarea, select {
  font: inherit;
}

p, h1, h2, h3, h4, h5, h6 {
  overflow-wrap: break-word;
}
/* RESET END */


html {
 background-color: #f0f0f0; 
 color: #333; 
 font-family: Arial, sans-serif; 
 font-size: 1rem;
}

pre {
 border: 1px solid #ddd;
 padding: 1rem;
 overflow-x: auto;
 border-radius: 6px;
 padding: 1rem;
}
```

Nå har jeg mye mer kontroll over stilsettingen, og det er mye enklere å vedlikeholde:

{{ figure "ressursmappe.png" "Etablering av stilark" "Skjermbilde av etablering av stilark" }}

### Scriban

I forbindelse med stilsettingen la jeg merke til at `pre`-feltene hadde så store innrykk at det ble vanskelig å lese koden. Jeg prøvde å sette `margin: 0`, `padding: 0`, og `tab-size` men det hjalp ikke. Det viste seg å være Scriban som la til innrykk i koden.

Måten å få det bort på var å bruke tilde `~` i malen for variabelen:

```html
 {{~ content ~}}
```

Jeg lærte forøvrig også at CamelCase i Scriban blir separert med understrek:

```html
 {{~ for post in sitecontent.published_slugs | array.sort "published" | array.reverse  ~}}
```

### SiteContent

Sist men ikke minst la jeg merke til at innleggene på forsiden ikke reflekterte de endringene jeg hadde gjort i postene. Det viste seg at jeg hadde glemt å filtrere ut poster som: 

- ikke var publisert
- var merket som draft
- ikke hadde slug.

Jeg la inn et felt for dette i `SiteContent` kalt `PublishedSlugs`:

```csharp
namespace CsharpBlogg.Generator.Models
{
    internal class SiteContent
    {
        public SiteConfig SiteConfig { get; set; } = new SiteConfig();
        public List<Post> Posts { get; set; } = [];
        public List<Page> Pages { get; set; } = [];
        public List<ContentItem> PostsAndPages => new List<ContentItem>(Posts).Concat(Pages).ToList();
        public List<ContentItem> PublishedSlugs => new List<ContentItem>(Posts).Concat(Pages).Where(c => c.Published != null && c.Published <= DateTime.Now && !string.IsNullOrEmpty(c.Slug) && !c.Draft).ToList();
        public List<string> AllTags { get; set; } = [];

        public SiteContent(SiteConfig siteConfig, List<Post> posts, List<Page> pages)
        {
            SiteConfig = siteConfig;
            Posts = posts;
            Pages = pages;
            AllTags = [.. posts.SelectMany(p => p.Tags).Distinct()];
        }
    }
}
```

Jeg måtte da også oppdatere `ContentConverter` og malen til å benytte denne versjonen.

Det er mye å tenke på i et SSG-prosjekt, men det er også veldig lærerikt. Jeg har lært mye om C#, Scriban, og hvordan man kan strukturere et prosjekt som dette.


