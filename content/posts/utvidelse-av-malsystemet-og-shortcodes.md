---
title: Maler i maler, og introduksjon av "shortcodes"
published: 2026-11-22
tags: [ssg-prosjekt, csharp, scriban]
---

**Malsystemet til Scriban gir svært mange muligheter. Alt fra grunnleggende standardmaler for alt innhold, til å ha maler i maler, og små maler angitt direkte i markdown. Det gir oss muligheten til å skape en nettside med dynamisk utseende og innhold.**

Å ha én standardmal for alt innhold, eller å lage separate maler for ulikt type innhold med felles elementer som header og footer, blir fort veldig uhåndterlig og uoversiktlig.

Derfor har Scriban gjort det mulig å flette maler inn i hverandre med `TemplateLoader`. Det åpner for mange muligheter, men jeg må innrømme at jeg brukte en del tid på å forstå hvordan det fungerer, og hvordan etablere funksjonen.

## Maler i maler

Fra før av hadde jeg etabler en grunnleggende standardmal, som inkluderte elementene `header` og `footer`.

```html
<!DOCTYPE html>
<html>
<head>
	<meta charset="UTF-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>{{ title }}</title>
	<link rel="stylesheet" href="resources/css/style.css" />
</head>
<body>
	{{ include "general/header.html" }}
	<div class="main">
        {{~ main ~}}
    </div>
    {{ include "general/footer.html" }}
</body>
</html>
```

Det som er viktig å forstå her, er at `{{~ main ~ }}` i mitt tilfelle fungerer som en plassholder for innholdet som skal renderes. 

Det kan være teksten i en markdown-fil, men det kan også være innholdet i en annen mal. Det er på denne måten vi kan ha maler i maler, og dermed skape et dynamisk utseende på nettsiden.

Per nå har jeg to ulike maler. En for forsiden som lister blogginnlegg, og en for selve blogginnleggene. Begge malene inkluderer header og footer, men innholdet i `main` er forskjellig.

Det vil si at det dersom det som skal injiseres i main også er en mal, må den leses og renderes først, før den sendes til den overordnede malen. Man går innenfra og ut.

## Shortcodes

Shortcodes er en annen måte å injisere innhold i malene på. Det er små maler som kan brukes direkte i markdown-filer, og som kan ha parametere.

Det er en veldig god funksjon for å skape et mer dynamisk innhold, med et utseende som kan endres ett sted, og deles mellom alle typer innhold. 

Eksempler på shortcodes kan være youTube-embed, fremhevede blokker, tabeller, knapper, eller andre elementer som skal ha et bestemt utseende.

Her er et eksempel på en shortcode for et bildefelt, som kan brukes i markdown-filer:

```html
Her er resultatet:

{{ figure "hello-world.png" "Min første HTML-side basert på Markdown" }}

Allerede her var det mye å lære.
```

Og her er den tilsvarende malen for shortcoden `figure`:

```html
 {{- src = $0; caption = $1; alt = $2 ?? $1 -}}
 {{- if src -}}
 <figure>
 <img src="resources/img/{{ src }}" alt="{{ alt | html.escape }}" />
 {{- if caption }}
 <figcaption>{{ caption }}</figcaption>
 {{- end }}
 </figure>
 {{- end -}}
```

Måten dette fungerer på, er at under konverteringen av markdown til HTML, så blir alle shortcode-ene funnet, og registrert som funksjoner i Scriban. 

Deretter blir markdown-filene evaluert, og ved match mellom shortcode-navnet og funksjonsnavnet,  blir funksjonen kjørt, og innholdet i malen injisert i markdown-filen.

## Koden

Her følger et utdrag av koden som gjør dette mulig. Den er lagt inn i `ContentConverter`-klassen, og kjøres for hvert innholdselement som skal konverteres.

Jeg går gjennom del for del under.

```csharp
internal static class ContentConverter
{
    internal static void WriteContentToHtml(SiteContent siteContent)
    {

        // ...

        // Oppretter en instans av loaderen med rotmappen for malene
        var templateLoader = new MyIncludeFromDisk(siteContent.SiteConfig.TemplatesPath);

        // Oppretter en TemplateContext og fester loaderen til den
        var context = new TemplateContext
        {
            TemplateLoader = templateLoader
        };

        // Legger til globale variabler som kan brukes i malene
        var scriptObject = new ScriptObject
        {
            { "basepath", siteContent.SiteConfig.BasePath }, // Bane for index.
            { "title", siteContent.SiteConfig.Title }, // Tittel for siden
            { "sitecontent", siteContent }, // Hele innholdet
            { "contentitem", contentItem } // Innholdet som skal rendres
        };

        context.PushGlobal(scriptObject);

        // Finner mappen for shortkodes
        var shortcodesDir = Path.Combine(siteContent.SiteConfig.TemplatesPath, "shortcodes");
        
        if (Directory.Exists(shortcodesDir))
        {
            // Itererer over hver fil i mappen, og lager en Scriban-funksjon
            foreach (var file in Directory.GetFiles(shortcodesDir, "*.html"))
            {
                var funcName = Path.GetFileNameWithoutExtension(file);

                var htmlContent = File.ReadAllText(file);

                // Merk {{ ... }} rundt selve funksjonsdefinisjonen og capture!
                var wrapper =
                $$$"""
                    {{ func {{{funcName}}}(a0 = null, a1 = null, a2 = null, a3 = null, a4 = null, a5 = null)
                        $0 = a0; $1 = a1; $2 = a2; $3 = a3; $4 = a4; $5 = a5
                    capture __sc_out }}
                        {{{htmlContent}}}
                    {{ end
                        ret __sc_out
                    end }}
                """;

                // Behandle wrapperen
                var funcTemplate = Template.Parse(wrapper);
                if (funcTemplate.HasErrors)
                {
                    throw new Exception($"Feil i shortcode '{file}': {string.Join(", ", funcTemplate.Messages)}");
                }
                // Evaluer innholdet i shortcode-ene
                funcTemplate.Evaluate(context);
            }
        }

        // Se om det eksisterer noe markdown
        string markdownBody = contentItem.MarkdownBody ?? string.Empty;

        // Kodeblokk for å finne shortcodes i markdown-teksten, og matche med Scriban-funksjon 
        // med samme navn, etablert over
        if (!string.IsNullOrWhiteSpace(markdownBody))
        {
            // Evaluer kun korte single-line tagger som {{ figure ... }} (ignorerer flerlinjes kodeeksempler og {{ for }})
            markdownBody = System.Text.RegularExpressions.Regex.Replace(
                markdownBody,
                @"^\{\{\s*(?!for\b|if\b|end\b|else\b|raw\b|endraw\b)([^\r\n\}]+)\}\}\s*$",
                match =>
                {
                    var tpl = Template.Parse(match.Value);
                    return tpl.HasErrors ? string.Empty : tpl.Render(context);
                },
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
        }

        // Konvertere markdown til HTML, og injiser i scriptObject
        contentItem.Html = Markdig.Markdown.ToHtml(markdownBody, markdownPipeline);
        scriptObject["main"] = contentItem.Html;

        // Finn grunnleggende mal
        var baseTemplate = new DirectoryInfo(siteContent.SiteConfig.TemplatesPath)
                .GetFiles("general/default.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
        
        // Sjekke etter egendefinert mal
        var customTemplate = new DirectoryInfo(siteContent.SiteConfig.TemplatesPath)
                .GetFiles(contentItem.Template, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
        
        // Hvis egendefinert mal, så behandle den
        if (customTemplate != null)
        {
            string customTemplateContent = "";
            customTemplateContent = File.ReadAllText(customTemplate.FullName);
            var parsedCustomTemplate = Template.Parse(customTemplateContent); // Parse innholdet til en Scriban-mal
            scriptObject["main"] = parsedCustomTemplate.Render(context); // Render malen med konteksten som inneholder variablene
        }

        string content = File.ReadAllText(baseTemplate.FullName);
        var parsedTemplate = Template.Parse(content); // Parse innholdet til en Scriban-mal

        html += parsedTemplate.Render(context); // Render malen med konteksten som inneholder variablene

        contentItem.Html = html;

        SaveFile.SaveFilesToDisk(siteContent.SiteConfig, contentItem);

        }

    }
}
```

### Oppset av konteksten og funksjoner for malene

I koden under etablerer jeg en `TemplateContext`, som brukes til å evaluere Scriban-maler. Jeg setter også opp en `TemplateLoader` som peker til mappen der malene ligger.

Deretter legger jeg til globale variabler som kan brukes i malene, som `basepath`, `title`, `sitecontent` og `contentitem`. Dette gjør det mulig å bruke disse variablene direkte i malene uten å måtte sende dem inn som parametere.

```csharp
// Oppretter en instans av loaderen med rotmappen for malene
var templateLoader = new MyIncludeFromDisk(siteContent.SiteConfig.TemplatesPath);

// Oppretter en TemplateContext og fester loaderen til den
var context = new TemplateContext
{
    TemplateLoader = templateLoader
};

// Legger til globale variabler som kan brukes i malene
var scriptObject = new ScriptObject
{
    { "basepath", siteContent.SiteConfig.BasePath }, // Bane for index.
    { "title", siteContent.SiteConfig.Title }, // Tittel for siden
    { "sitecontent", siteContent }, // Hele innholdet
    { "contentitem", contentItem } // Innholdet som skal rendres
};

context.PushGlobal(scriptObject);
```

### Iterering over shortcodes og oppretting av Scriban-funksjoner

Neste steg er å finne mappen for shortcodes, og iterere over hver fil i mappen. 

For hver fil opprettes en Scriban-funksjon med samme navn som filen (uten filendelsen). Og HTML-koden pakkes inn i Scriban-funksjonen, slik at den kan brukes som en shortcode i markdown-filer.

Merk wrapperen som opprettes rundt funksjonen. Det er Scribans måte å definere funksjoner på. Den inkluderer `capture` for å fange opp innholdet som skal returneres, og setter opp parametere som kan brukes i malen. I dette tilfellet har jeg satt opp 6 parametere (a0 til a5), som kan brukes i malen for shortcoden.

```csharp
// Finner mappen for shortkodes
var shortcodesDir = Path.Combine(siteContent.SiteConfig.TemplatesPath, "shortcodes");

// Sjekker om mappen eksisterer
if (Directory.Exists(shortcodesDir))
{
    // Itererer over hver fil i mappen, og lager en Scriban-funksjon
    foreach (var file in Directory.GetFiles(shortcodesDir, "*.html"))
    {
        // Oppretter funksjonsnavnet basert på filnavnet
        var funcName = Path.GetFileNameWithoutExtension(file);
        // Leser innholdet i filen
        var htmlContent = File.ReadAllText(file);

        // Oppretter en wrapper for funksjonen, som inkluderer innholdet i filen.
        var wrapper =
        $$$"""
            {{ func {{{funcName}}}(a0 = null, a1 = null, a2 = null, a3 = null, a4 = null, a5 = null)
                $0 = a0; $1 = a1; $2 = a2; $3 = a3; $4 = a4; $5 = a5
            capture __sc_out }}
                {{{htmlContent}}}
            {{ end
                ret __sc_out
            end }}
        """;

        // Konvertere wrapperen til en Scriban-mal, og sjekke for feil
        var funcTemplate = Template.Parse(wrapper);
        if (funcTemplate.HasErrors)
        {
            throw new Exception($"Feil i shortcode '{file}': {string.Join(", ", funcTemplate.Messages)}");
        }
        // Evaluer innholdet i shortcode-ene med konteksten som inneholder de globale variablene
        funcTemplate.Evaluate(context);
    }
}
```

### Evaluering av shortcodes i markdown

Nå som alle shortcodes er registrert som funksjoner i Scriban, kan vi evaluere markdown-innholdet for å finne og erstatte shortcodes med innholdet fra de tilsvarende malene.

Her brukes en regex for å finne alle korte tagger på en linje som `{{ figure ... }}` i markdown-teksten. Flerlinjede kodeeksempler og kontrollstrukturer som `{{ for }}` blir ignorert.

Ved match erstattes altså shortcoden med innholdet fra den tilsvarende Scriban-funksjonen, som rendres med konteksten som inneholder de globale variablene.

```csharp
// Se om det eksisterer noe markdown
string markdownBody = contentItem.MarkdownBody ?? string.Empty;

// Kodeblokk for å finne shortcodes i markdown-teksten, og matche med Scriban-funksjon 
// med samme navn, etablert over
if (!string.IsNullOrWhiteSpace(markdownBody))
{
    // Evaluer kun korte single-line tagger som {{ figure ... }} (ignorerer flerlinjes kodeeksempler og {{ for }})
    markdownBody = System.Text.RegularExpressions.Regex.Replace(
        markdownBody,
        @"^\{\{\s*(?!for\b|if\b|end\b|else\b|raw\b|endraw\b)([^\r\n\}]+)\}\}\s*$",
        match => // For hver match, parse og render innholdet med konteksten som inneholder de globale variablene
        {
            var tpl = Template.Parse(match.Value);
            return tpl.HasErrors ? string.Empty : tpl.Render(context);
        },
        System.Text.RegularExpressions.RegexOptions.Multiline
    );
}
```

Akkurat denne blokken er jeg noe usikker på, men foreløpig så ser det ut til å virke. Men hvis jeg ønsker å vise en enkel Scriban-kommando i en kodeblokk nå, må jeg bruke `{{ raw }} ... {{ endraw }}` for å unngå at den blir evaluert, eller legge til ett mellomrom foran.

### Kovertering av markdown til HTML og injisering i scriptObject

Siste steg er å gjøre en sjekk på om det skal brukes en egendefinert mal for innholdet, eller om det bare er standardmalen som skal benyttes.

Hvis siden ikke har en spesialmal (`customTemplate == null`), forblir `scriptObject["main"]` den rene HTML-en fra Markdown-filen (som nå også inneholder eventuelle shortcodes som har blitt erstattet med HTML). 

Hvis den har en spesialmal (f.eks. en mal for et blogginnlegg med forfatterboks og dato), rendres den spesialmalen først. Inni den spesialmalen står det `{{ main }}`, slik at Markdown-innholdet bakes inn i spesialmalen – og så lagres hele den pakken som den nye `main`.

Til slutt leses `default.html` (selve rammeverket rundt hele nettsiden, med `<html>`, `<head>`, meny og footer). Inni `default.html` står det også `{{ main }}`. Når `parsedTemplate.Render(context)` kjøres, dyttes alt inn i hovedmalen, og den komplette nettsiden lagres til disk.

Til slutt lagres innholdet på disk.

```csharp
// Konvertere markdown til HTML, og injiser i scriptObject
contentItem.Html = Markdig.Markdown.ToHtml(markdownBody, markdownPipeline);
scriptObject["main"] = contentItem.Html;

// Finn grunnleggende mal
var baseTemplate = new DirectoryInfo(siteContent.SiteConfig.TemplatesPath)
        .GetFiles("general/default.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
        
// Sjekke etter egendefinert mal
var customTemplate = new DirectoryInfo(siteContent.SiteConfig.TemplatesPath)
        .GetFiles(contentItem.Template, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
        
// Hvis egendefinert mal, så behandle den
if (customTemplate != null)
{
    string customTemplateContent = "";
    customTemplateContent = File.ReadAllText(customTemplate.FullName);
    var parsedCustomTemplate = Template.Parse(customTemplateContent); // Parse innholdet til en Scriban-mal
    scriptObject["main"] = parsedCustomTemplate.Render(context); // Render malen med konteksten som inneholder variablene
}

string content = File.ReadAllText(baseTemplate.FullName);
var parsedTemplate = Template.Parse(content); // Parse innholdet til en Scriban-mal

html += parsedTemplate.Render(context); // Render malen med konteksten som inneholder variablene

contentItem.Html = html;

SaveFile.SaveFilesToDisk(siteContent.SiteConfig, contentItem);
```

Under følger den komplette koden for `ContentConverter`-klassen, som nå inkluderer alt som er beskrevet over.
```csharp
using CsharpBlogg.Generator.Models;
using Markdig;
using Markdown.ColorCode;
using Scriban;
using Scriban.Runtime;

namespace CsharpBlogg.Generator.Services
{
    internal static class ContentConverter
    {
        internal static void WriteContentToHtml(SiteContent siteContent)
        {

            foreach (var contentItem in siteContent.PublishedSlugs)
            {
                Console.WriteLine($"Converting {contentItem.Filename} to HTML...");

                //
                // TODO: Flytt markdownPipeline til globalt sted, slik at det ikke må gjøres for hver fil?
                //
                var markdownPipeline = new MarkdownPipelineBuilder()
                    .UseYamlFrontMatter() // Hente ut YAML. Trengs ikke her?
                                          //.UseAdvancedExtensions()
                    .UseColorCode()
                    .Build(); // Bygge rørgaten.


                var html = string.Empty;

                // Definer template for innholdet, enten fra YAML eller default.
                if (string.IsNullOrEmpty(contentItem.Template)) // Hvis YAML er tomt
                {
                    var pathcheck = File.Exists(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), contentItem.Filename + ".html"));
                    //Console.WriteLine($"Defining template for {contentItem.Filename} in path: {file} -- {Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), contentItem.Filename + ".html")}");
                    if (pathcheck) // Hvis template finnes i templates/<contentType>/<filename>.html, bruk den.
                    {
                        //var file = Directory.EnumerateFiles(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant()), contentItem.Filename + ".html").FirstOrDefault();
                        contentItem.Template = Path.Combine(contentItem.GetType().Name.ToLowerInvariant(), contentItem.Filename + ".html");
                    }
                    else // Hvis ikke, sjekk om default.html finnes i templates/<contentType>/default.html
                    {
                        var pathCheck = File.Exists(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), "default.html"));
                        //Console.WriteLine($"Defining template for {contentItem.Filename} in path: {file2} -- {Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), "default.html")}");
                        if (pathCheck) // Hvis default.html finnes i templates/<contentType>/default.html, bruk den.
                        {
                            // var file2 = Directory.EnumerateFiles(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant()), "default.html").FirstOrDefault();
                            contentItem.Template = Path.Combine(contentItem.GetType().Name.ToLowerInvariant(), "default.html");
                        }
                        else // Hvis ikke, bruk templates/general/default.html
                        {
                            //contentItem.Template = Path.Combine("general/default.html");
                        }
                    }
                }
                else // Hvis YAML har definert en template, sjekk om den finnes i templates/<contentType>/<template>.html
                {
                    var file3 = Directory.EnumerateFiles(Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant()), contentItem.Template + ".html").FirstOrDefault();
                    Console.WriteLine($"Defining template for {contentItem.Filename} in path: {file3} -- {Path.Combine(siteContent.SiteConfig.TemplatesPath, contentItem.GetType().Name.ToLowerInvariant(), contentItem.Template + ".html")}");
                    if (file3 != null) // Hvis template finnes i templates/<contentType>/<template>.html, bruk den.
                    {
                        contentItem.Template = Path.Combine(contentItem.GetType().Name.ToLowerInvariant(), contentItem.Template + ".html");
                    }
                    else // Hvis ikke, bruk templates/general/default.html
                    {
                        //contentItem.Template = Path.Combine("general/default.html");
                    }
                }


                // 1. Opprett en instans av loaderen med rotmappen for malene dine
                var templateLoader = new MyIncludeFromDisk(siteContent.SiteConfig.TemplatesPath);

                // 2. Opprett en TemplateContext og fest loaderen til den
                var context = new TemplateContext
                {
                    TemplateLoader = templateLoader
                };

                // 3. Legg til globale variabler som kan brukes i malene
                var scriptObject = new ScriptObject
                {
                    { "basepath", siteContent.SiteConfig.BasePath }, // Bane for index.
                    { "main", contentItem.Html }, // Innhold fra markdown-filen
                    { "title", siteContent.SiteConfig.Title }, // Tittel for innholdet
                    { "sitecontent", siteContent },
                    { "contentitem", contentItem } // Innholdet som skal rendres
                };

                context.PushGlobal(scriptObject);

                var shortcodesDir = Path.Combine(siteContent.SiteConfig.TemplatesPath, "partials");
                if (Directory.Exists(shortcodesDir))
                {
                    foreach (var file in Directory.GetFiles(shortcodesDir, "*.html"))
                    {
                        var funcName = Path.GetFileNameWithoutExtension(file);
                        var htmlContent = File.ReadAllText(file);

                        // Merk {{ ... }} rundt selve funksjonsdefinisjonen og capture!
                        var wrapper =
                        $$$"""
                            {{ func {{{funcName}}}(a0 = null, a1 = null, a2 = null, a3 = null, a4 = null, a5 = null)
                                $0 = a0; $1 = a1; $2 = a2; $3 = a3; $4 = a4; $5 = a5
                            capture __sc_out }}
                                {{{htmlContent}}}
                            {{ end
                                ret __sc_out
                            end }}
                        """;

                        var funcTemplate = Template.Parse(wrapper);
                        if (funcTemplate.HasErrors)
                        {
                            throw new Exception($"Feil i shortcode '{file}': {string.Join(", ", funcTemplate.Messages)}");
                        }
                        funcTemplate.Evaluate(context);
                    }
                }

                string markdownBody = contentItem.MarkdownBody ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(markdownBody))
                {
                    // Evaluer kun korte single-line tagger som {{ figure ... }} (ignorerer flerlinjes kodeeksempler og {{ for }})
                    markdownBody = System.Text.RegularExpressions.Regex.Replace(
                        markdownBody,
                        @"^\{\{\s*(?!for\b|if\b|end\b|else\b|raw\b|endraw\b)([^\r\n\}]+)\}\}\s*$",
                        match =>
                        {
                            var tpl = Template.Parse(match.Value);
                            return tpl.HasErrors ? string.Empty : tpl.Render(context);
                        },
                        System.Text.RegularExpressions.RegexOptions.Multiline
                    );
                }

                // Konverterer markdown til HTML
                contentItem.Html = Markdig.Markdown.ToHtml(markdownBody, markdownPipeline);
                
                // Legger til innholdet fra markdown-filen i konteksten
                scriptObject["main"] = contentItem.Html;

                var baseTemplate = new DirectoryInfo(siteContent.SiteConfig.TemplatesPath)
                        .GetFiles("general/default.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

                var customTemplate = new DirectoryInfo(siteContent.SiteConfig.TemplatesPath)
                        .GetFiles(contentItem.Template, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

                if (customTemplate != null)
                {

                    string customTemplateContent = "";
                    customTemplateContent = File.ReadAllText(customTemplate.FullName);
                    var parsedCustomTemplate = Template.Parse(customTemplateContent); // Parse innholdet til en Scriban-mal
                    scriptObject["main"] = parsedCustomTemplate.Render(context); // Render malen med konteksten som inneholder variablene
                }

                //content = File.ReadAllText(baseTemplate.FullName); // Les innholdet i malen, eller default.html hvis malen ikke finnes.
                string content = File.ReadAllText(baseTemplate.FullName);
                var parsedTemplate = Template.Parse(content); // Parse innholdet til en Scriban-mal
                html += parsedTemplate.Render(context); // Render malen med konteksten som inneholder variablene

                //html += contentItem.Html; // Innhold fra markdown-filen
                contentItem.Html = html;
                //Console.WriteLine(contentItem.Html);

                SaveFile.SaveFilesToDisk(siteContent.SiteConfig, contentItem);
            }

        }
    }
}
```