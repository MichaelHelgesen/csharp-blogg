---
published: 2026-11-09
title: Implementering av malmotor
tags: ["csharp", "ssg-prosjekt", "scriban"]
draft: true
---

**For å koble sammen innhold og design trenger jeg å implementere en malmotor. Å utvikle det selv er for avansert for meg på mitt nåværende nivå, så valget falt på [Scriban](https://scriban.github.io/).**

Mitt SSG-prosjekt har nå kommet til et punkt hvor jeg må koble sammen innhold og design. Jeg har allerede laget en del innhold i form av markdown-filer, men for å gjøre nettsiden funksjonell trenger jeg å implementere en malmotor.

Det var flere å velge mellom, men Scriban virket som et godt valg for meg. Det er en enkel og lettvekts malmotor som er skrevet i C#, som kommer med mange anbefalinger og vedlikeholdes aktivt.

Målet var å opprette en forside som viser en liste over alle innlegg, som kan trykkes på, samt å implementere et felles malelement som header. Dette vil gjøre det mulig å begynne å bruke nettsiden, selv om den er ekstremt basic.

Alle oppgaver i del 3 av fase 1 ble gjennomført i denne økten:

- [x] Bestemme template engine
- [x] Logikk for å opprette sider
- [x] Logikk for å koble sider til templates
- [x] Implementere template engine
	- [x] page
	- [x] post
	- [x] generelle (header/footer)


## Scriban

Jeg installerte Scriban via NuGet:

```csharp
dotnet add package Scriban
``` 

Det gir meg muligheten til å mikse HTML og C# via HTML-maler for både sider og innlegg, og generelle maler som header og footer samt andre små gjenbrukbare komponenter.

## Maler

Jeg startet med forsiden, ved å opprette en `index.html` i `/templates`. Den inneholder en enkel liste over alle innlegg, som kan trykkes på.

```html
Dette er en template.
Hello {{ name }}!
<ul>
{{~ for post in posts ~}}
  <li><span>{{ post.created }}</span><a href="{{ post.slug }}">{{ post.title }}</a>
	<ul>
	{{~ for tag in post.tags ~}}
		<li>{{ tag }}</li>
	{{~ end ~}}
	</ul>
  </li>
{{~ end ~}}
</ul>
```

Jeg laget også en enkelt `header.html` i `/templates/general`, som skal brukes på alle sider. Den inneholder en enkel header med en lenke tilbake til forsiden.

```html
<header>
    <h1><a href="{{ basepath }}">My Homepage</a></h1>
</header>
```

Her er et enkelt eksempel på hvordan en slik header-mal fungerer:

```csharp
var header = GetFiles(siteConfig.TemplatesPath, "general/header.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
string content = File.ReadAllText(header.FullName);
var templateTest = Template.Parse(content);
html += templateTest.Render(new { basepath = siteConfig.BasePath });
```

Først henter jeg header-malen.

Deretter leser jeg innholdet i filen, og så parser jeg innholdet med Scriban. Det betyr at Scriban leser innholdet og ser etter felter som `{{ basepath }}` for å kunne erstatte dem med verdier.

Til slutt render jeg malen, og sender inn verdien for `basepath` som skal erstatte `{{ basepath }}` i malen.

Hvordan maler skal struktureres og organiseres er ikke helt klart for meg enda, men jeg antar at det vil bli tydeligere etter hvert som jeg implementerer flere maler og funksjoner.

## Sider

Koden lager sider av alle markdown-filer i `/content/post`. For å gjøre det samme for pages, utvidet jeg SiteConfig til å inkludere sider og maler:

```csharp
namespace CsharpBlogg.Generator.Models;

public class SiteConfig
{
    public string ContentPath { get; set; } = "content/posts";
    public string OutputPath { get; set; } = "output";
    public string PagesPath { get; set; } = "content/pages";
    public string TemplatesPath { get; set; } = "templates";
}
```

Deretter kopierte jeg logikken fra opprettelsen av poster til også å gjelde sider, og la til enkelte funksjoner. 

All koden er kjapt gjort, og langt fra god. Det fikser jeg senere. Jeg ville bare få det til å virke.

Jeg startet med å hente malene:

```csharp
// Maler
var templates = GetFiles(siteConfig.TemplatesPath, "*.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });
```

Så itererte jeg over sidene, på samme måte som postene:

```csharp
// Sider
if (!Directory.Exists(siteConfig.PagesPath))
{
    Console.WriteLine($"Pages folder ({siteConfig.ContentPath}) is missing or not accessible. Are you in the correct working directory?: {Directory.GetCurrentDirectory()}");
    return;
}

var pages = GetFiles(siteConfig.PagesPath, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

foreach (var item in pages)
{
    string pageFileName = item.Name.Split(".")[0];
    var fileAsString = File.ReadAllText(item.FullName); // Leser filen

    var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.

    var post = new Post();

    var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
    if (yamlBlock != null)
    {
        var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
        post = yamlDeserializer.Deserialize<Post>(yaml) ?? post; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
    }

    var html = Markdown.ToHtml($"# {post.Title}", markdownPipeline); // Tittel fra YAML
    html += Markdown.ToHtml(markdownTree, markdownPipeline); // Tekst til HTML

    if (post.Slug == null || post.Slug == "")
    {
        post.Slug = slugConverter(item.Name.Split(".")[0]);
    }
    else
    {
        post.Slug = slugConverter(post.Slug);
    }

    if (post.MetaTitle == null || post.MetaTitle == "")
    {
        post.MetaTitle = post.Title;
    }

    post.Html = html;
```

Så ville jeg prøve å sette inn en mal. Jeg tenkte ikke spesielt nøye gjennom struktur og slikt, så jeg bare matchet sidenavnet mot malnavnet. Ikke spesielt bra eller solid. 

Ved treff så leser jeg filen, deretter kobler jeg på Scriban og parser HTML-innholdet, og injiserer kode.

```csharp
    // Sette inn mal
    foreach (var template in templates)
    {
        if (template.Name.Split(".")[0] == pageFileName)
        {
            string content = File.ReadAllText(template.FullName);
            var templateTEst = Template.Parse(content);
            var result = templateTEst.Render(new { Name = "World", posts = postList });

            post.Html += result;
        }
    }

    // Skriv til HTML
    File.WriteAllText(Path.Combine(siteConfig.OutputPath, Path.ChangeExtension(post.Slug, "html")), post.Html);
}
```

Malen var som vist oppsatt på følgende måte:

```html
Dette er en template.
Hello {{ name }}!
<ul>
{{~ for post in posts ~}}
  <li><span>{{ post.created }}</span><a href="{{ post.slug }}">{{ post.title }}</a>
	<ul>
	{{~ for tag in post.tags ~}}
		<li>{{ tag }}</li>
	{{~ end ~}}
	</ul>
  </li>
{{~ end ~}}
</ul>
```

Og det fungerte! Jeg fikk en liste over alle innlegg på forsiden, som kan trykkes på.

## Header-mal

Jeg ville teste Scriban videre. Jeg må jo ha noen generelle mal-deler som skal være felles for alle sider, slik som header og footer. 

Så jeg etablerte mapper `templates`: `posts`, `pages` og `general`. 

Under general opprettet jeg `header.html`:

```html
<header>
	<h1><a href="index.html">My homepage</a></h1>
</header>
```

Så prøvde jeg å legge dem inn på alle sider:

```csharp
var header = GetFiles(siteConfig.TemplatesPath, "general/header.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

string content = File.ReadAllText(header.FullName);

var templateTEst = Template.Parse(content);

html += templateTEst.Render();

html += Markdown.ToHtml($"# {post.Title}", markdownPipeline); // Tittel fra YAML
html += Markdown.ToHtml(markdownTree, markdownPipeline); // Tekst til HTML
```

Kaotisk og dårlig kode, men det virket også. Og det var så gøy!:

Nå kan alle blogginnlegg lenke tilbake til forsiden.

Og med det er faktisk alt på plass for en første versjon av SSG-prosjektet. Jeg kan lage innhold, og det vises på nettsiden. Det er ikke mye, men det er en start.

## Andre endringer

Endret også på litt her og der, blant annet:
- Oppdaterte generering av slug
- La til `CreationDate` på `Post`-klassen.


## Lærte

- - Å bruke Scriban
- At Scriban bruker små bokstaver i template. Jeg skjønte nemlig ikke hvorfor `post.Title` ikke virket.

## Neste steg

Nå som grunnlaget er på plass, og alle oppgavene i del 3 av fase 1 er gjennomført, kan jeg begynne på del 4, som handler om å publisere siden, og rydde og effektivisere koden.

Det vil bli et mer omfattende delmål, men jeg føler at struktur, designmønster, og generell arkitektur og opprydning er et viktig neste steg for å hindre at prosjektet blir uoversiktlig og vanskelig å jobbe med.

Kommende oppgaver i del 4 av fase 1:

_Struktur_
- [ ] Dele `Program.cs` i tydelige steg: 
	- [ ] Last konfigurasjon
	- [ ] Les innhold
	- [ ] Lag HTML
	- [ ] Skriv filer
- [ ] Skille _innsamling_ fra _skriving_: først alle `ContentItem`-objekter i lister, deretter sidene
- [ ] Flytte `LoadConfig` til `SiteConfig.Load()` eller en egen klasse
- [ ] Flytte `Slugify` til `Utilities/`
- [ ] Én felles metode for å lese både sider og innlegg

_Rydding_
- [ ] Konsekvente namespaces, tilgangsmodifikatorer og `static` der det passer
- [ ] Fjerne død kode og utkommentert kode
- [ ] Gå gjennom navn og kommentarer
- [ ] Rydde opp i `BasePath` etter at konfigurasjonen er lest

_Publisering_
- [ ] Bruke `BasePath` i alle lenker i malene
- [ ] Arbeidsflyt i GitHub Actions som bygger og publiserer
- [ ] Slå på GitHub Pages med Actions som kilde