---
title: Mer om polymorfisme og objektorientert programmering
published: 2026-11-17
tags: [csharp, SSG-prosjekt]
---

**I dag flikket og flyttet jeg på den eksisterende koden, og fikk enda en påminner om viktigheten av å plassere ansvar og logikk på riktig sted i objektorientert programmering.**

## Innkapsling av data ("encapsulation")

Jeg startet økten med å gruble over hvordan jeg skulle få tak i alle behandlede poster og sider i den generiske metoden `ContentConverter` for å konvertere innholdet til HTML-filer.

Problemet mitt var at jeg kalte `ContentReader` for å hente postene derson den gjeldende `contentItem` var en `Page`. 

```csharp
//...

if (contentItem.GetType().Name == "Page")
{
    var posts = ContentReader.LoadContent<Post>(siteConfig.PostFolderPath);

//...
```

Men jeg hadde jo allerede hentet alle postene i `Program.cs`, og den listen hadde allerede blitt sendt gjennom `ContentConverter`, som også utførte operasjoner på alle postene. 

Ved å kalle dem på nytt, så endte jeg med et varierende resultat på nettsiden. Listen over poster viste ikke de samme formateringene som på sidene. Tag-ene var for eksempel ikke normalisert til små bokstaver, og jeg hadde ikke sjekket om slug var satt.

Dessuten føltes det feil og unødvendig å kjøre `ContentReader`to ganger.

Men hvordan skulle jeg da få tak i alle postene i `ContentConverter`? Jeg kunne jo ikke sende dem som individuelle argumenter, for da måtte jeg ha en metode med et uendelig antall parametre.

Svaret var å lage en egen klasse `SiteContent`, som representerer et bilde av nettsidens innhold og struktur. En sortert liste over poster, en liste av tagger, en liste av sider, i tillegg til `SiteConfig` og annen informasjon:

```csharp
namespace CsharpBlogg.Generator.Models
{
    internal class SiteContent
    {
        public SiteConfig SiteConfig { get; set; } = new SiteConfig();
        public List<Post> Posts { get; set; } = [];
        public List<Page> Pages { get; set; } = [];
        public List<ContentItem> PostsAndPages => new List<ContentItem>(Posts).Concat(Pages).ToList();
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

Det er dette som i objektorientert programmering kalles innkapsling av data ("encapsulation"). Hele dette objektet kan sendes som et argument til `ContentConverter`, og da har jeg alt innholdet tilgjengelig der, uten å måtte kalle `ContentReader` på nytt.

Innholdet i objektet gjør at jeg også kan fjerne alle `if`-setningene (som helst ikke skal være der), i den generiske `ContentConverter`, som sjekket om det var en `Post` eller en `Page`. Nå får det riktig mal for hver type innhold automatisk, basert på informasjonen i objektet:

```csharp
Path.Combine(contentItem.GetType().Name.ToLowerInvariant(), contentItem.Filename + ".html");
```

### Polymorfisme

Deretter flyttet jeg `Normalize` fra `ContentConverter` og ut til Program i en for each:

```csharp
posts.ForEach(post => post.Normalize());
```

Men det føltes også litt malplasert. Først leser jeg alle sider og poster, også itererer jeg over dem en gang til, bare for å normalisere tagger. 

Det burde vel være en del av innlastingen av filene, og ikke noe som må gjøres etterpå?

Da kom jeg på at `Normalize` jo er en virtuell metode, og at jeg kan overstyre den i `Post` for å gjøre det som er nødvendig for den typen innhold. Den kan jo nettopp aktiveres under innlastingen i `ContentReader`.

```csharp
contentItem.Normalize(); // Normaliserer innholdet, f.eks. gjør alle tags til små bokstaver.
```

Det spiller ingen rolle om `contentItem` er en `Post` eller en `Page`, for `Post` overskriver metoden:

```csharp
public override void Normalize()
{
    base.Normalize();
    Tags = Tags.Select(tag => tag.ToLowerInvariant()).ToList(); // Gjør alle tags til små bokstaver
}
```

Det er dette som er polymorfisme i praksis. Jeg kan kalle `Normalize()` på et objekt av typen `ContentItem`, og så vil den riktige metoden bli kalt, basert på hvilken type objektet faktisk er.

### "Information Expert"

Objektorientert programmering handler også om å plassere ansvar og logikk på riktig sted. 

Dette krever erfaring, og er noe jeg må bruke tid på å lære. Men det er noen prinsipper som kan hjelpe meg på veien, og et av dem er "Information Expert".

Jeg ønsket å flytte logikken for å sette slug fra `LoadContent` i `ContentReader`, fordi metoden begynte å bli lang. Spørsmålet ble hvor? 

Den kunne etableres som egen metode i `ContentReader`, men da måtte jeg hentet alle dataene som trengtes fra `ContentItem` for å beregne slug, og deretter sende dem tilbake igjen, og det føltes feil. Metoden bør bo der dataene allerede eksisterer, altså i `ContentItem`.

Og nettopp dette er hva prinsippet "Information Expert" handler om:

> hvor ansvar og logikk skal plasseres – ikke primært hvor rådataene opprinnelig oppstår, men at metoden som behandler eller beregner noe bør bo i den samme klassen som allerede eier dataene som trengs for å gjøre jobben.

Så derfor etablerte jeg følgende metode i `ContentItem`:

```csharp
contentItem.SetSlug(); // Setter slug, enten fra YAML eller fra filnavnet.)

if (string.IsNullOrEmpty(contentItem.Slug))
{
    Console.WriteLine($"Filename {file.Name} with title {contentItem.Title} could not be converted to a slug. Skipping this file.");
    continue; // Hvis filnavnet ikke kan konverteres til en slug, hopp over denne filen.
}
```

Hvis Klasse A (`ContentItem`) har dataene, bør ikke Klasse B (`ContentReader`) hente ut alle dataene fra Klasse A, regne på dem, og sende svaret tilbake. Da har man skapt unødvendig kobling ("coupling") og spredd logikken utover. 

Klasse A er "informasjonseksperten" og bør gjøre beregningen selv.

### Spread fremfor Slice

Jeg hadde tidligere brukt `Slice` for å hente ut Markdown-teksten fra filen, men det er ikke nødvendig. 

Jeg kan bruke "spread" i stedet, som er mer lesbart og enklere å forstå:

```csharp
// Slice
//contentItem.Markdown = (string?)fileAsString.Slice(yamlBlock.Span.End, fileAsString.Length - yamlBlock.Span.End);

// Spread
contentItem.MarkdownBody = fileAsString[(yamlBlock.Span.End + 1)..]; // Markdown uten YAML.
```

Alt i alt en veldig lærerik økt, hvor jeg fikk en påminnelse om viktigheten av å plassere ansvar og logikk på riktig sted i objektorientert programmering.