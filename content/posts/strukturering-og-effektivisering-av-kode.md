---
published: 2026-11-11
title: Strukturering og effektivisering av kode: generics og polymorfisme i praksis
tags: ["csharp", "ssg-prosjekt"]
---

**Kodebasen i mitt SSG-prosjekt er ennå ganske liten, men begynner allerede å lukte (code smell). Derfor ville jeg strukturere og effektivisere koden før jeg går videre med nye funksjoner.**

I tillegg til generell retting av dårlig kode og struktur, var det enkelte andre ting jeg ønsket å utforske:

- Hvilke designprinsipper kan jeg bruke for å gjøre koden mer robust og fleksibel?
- En felles baseklasse for innlegg og sider, som kan håndtere felles logikk
- Etablere en mer strukturert og tydelig pipeline for å lese innhold, generere HTML og skrive til fil
- Undersøke generics og polymorfisme for å håndtere både innlegg og sider i samme generator
- Vurdere et enkelt context-objekt som kan sendes med til metoder som trenger det, i stedet for å bruke statiske og globale variabler

## Arv og baseklasse

Siden både innlegg og sider har mange felles felter og logikk, valgte jeg å lage en baseklasse `ContentItem`:

```csharp
namespace CsharpBlogg.Generator.Models;

public class ContentItem
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
    
    public virtual void Normalize()
    {   
    }
}
```

Metoden `Normalize()` er ment å håndtere felles logikk for alle typer som arver fra `ContentItem`, som å generere slug og sette standardverdier. Den er merket som `virtual`, slik at subklasser kan overstyre den med spesifikk logikk. 

Det kalles polymorfisme, et viktig prinsipp i objektorientert programmering. Det handler om at et objekt kan ha flere former, og gjør at jeg kan kalle `Normalize()` på et `ContentItem`-objekt og dens arvinger, og det er objektets faktiske type som avgjør hvilken versjon av metoden som kjøres.

I praksis betyr det at en metode kan ha forskjellige implementasjoner avhengig av hvilken type objekt som kaller den.

Det som er viktig å merke seg er at jeg kaller `base.Normalize()` i subklassene. Det betyr at jeg først kjører logikken i baseklassen, og deretter legger til spesifikk logikk for subklassen.
Dette må gjøres for å sikre at all felles logikk i baseklassen kjøres, samtidig som subklassene kan legge til sin egen logikk.

Merk at metoden i arvingene har erklæringsmodifikatoren `override`, som forteller C# at denne metoden overstyrer baseklassens versjon.

Om dette er beste måten å gjøre det på er jeg foreløpig ikke helt sikker på, men jeg tror det er et greit sted å starte.

Klassene `Post` og `Page` arver nå fra `ContentItem`, og ser slik ut:

```csharp
namespace CsharpBlogg.Generator.Models;

public class Post : ContentItem
{
    public List<string> Tags { get; set; } = new List<string>();

    public override void Normalize()
    {
        base.Normalize();
        Tags = Tags.Select(tag => tag.ToLowerInvariant()).ToList(); // Gjør alle tags til små bokstaver
    }
}

public class Page : ContentItem
{
    public override void Normalize()
    {
        base.Normalize();
        // Spesifikk logikk for sider
    }
}
```

## Sider og innlegg i samme generator

I og med at filprosessen for både innlegg og sider er svært lik, ønsket jeg å håndtere begge typer innhold i samme generator. Dette ville gjøre koden mer konsistent og enklere å vedlikeholde.

Jeg startet med å opprette en enum `ContentType` med verdiene `Post` og `Page`, som jeg kunne bruke for å skille mellom de to typene innhold, og la til et felt `ContentType` i `ContentItem`. Også brukte jeg mappenavnet for å bestemme typen:

```csharp
if (fileDirectory == siteConfig.PostFolder)
            {
                contentItem = new Post
                {
                    ItemType = ItemType.Post
                };
            }

            if (fileDirectory == siteConfig.PageFolder)
            {
                contentItem = new Page
                {
                    ItemType = ItemType.Page
                };
            }
``` 

Men det fant jeg fort ut at var unødvendig. Objektet vet nemlig selv hvilken type der er. Derfor slettet jeg enum-en og ContentType-feltet igjen.

Jeg fikk likevel problemer med å bruke `contentItem.GetType().Name;` opp mot `Deserialize<ContentItem>(fileContent);`. 

Det fungerte ikke, fordi `Deserialize` må returnere en kjent type ved kompilering. Jeg måtte derfor manuelt velge mellom `ContentItem`, `Post` eller `Page`. Dermed fikk jeg ikke med all informasjon om den egentlige typen. Jeg fikk for eksempel ikke med tags for innlegg, fordi `ContentItem` ikke har en `Tags`-property. Den ligger kun i `Post`. 

Dermed måtte jeg finne en annen løsning for å håndtere både innlegg og sider i samme generator.

Svaret ble å lage en generisk metode `LoadContent<T>()`.

## Generics (generiske typer og metoder)

Generics var ukjent for meg, så det måtte jeg lese meg opp på. 

Slik jeg forstår det handler det om å lage gjenbrukbar kode ved å sende inn datatypen som en parameter, på samme måte som man sender inn verdier som parametere.

I en metode som `LoadConent<Post>...` vil C# erstatte alle steder med `T` til fordel for `Post`.

Jeg etablerte følgende metode:

```csharp
static List<T> LoadContent<T>(SiteConfig siteConfig, string contentFolder) where T : ContentItem, new()
```

La oss gå gjennom del for del.

- `static List<T>`: Dette er erklæringsmodifikator og typen. Det forteller at denne metoden skal returnere en liste med datatyper `T`. Kalles metoden med typen `Post`, returneres `List<Post>`.
- `LoadContent<T>`: Navnet på metoden, samt at den tar datatype som parameter. Vinklene er det som forteller C# at dette er en generisk metode, og `T` er navnet på type-parameteren.
- `(SiteConfig siteConfig, string contentFolder)`: Øvrige parametre. I dette tilfellet `SiteConfig` og mappen til innholdet.
- `where T : ContentItem, new()`: Det viktigste punktet. Dette er en begrensning som bestemmer hva `T` skal være. Uten den har ikke programmet anelse om hva `T` er. Et bilde? En tekst? En spesifikk type?
    - `where T : ContentItem`: Arvebegrensning. Det forteller at `T` må være av typen `ContentItem`, eller en klasse som arver fra den.
    - `where T : new()`: Konstruktør-begrensning: `T` må ha en tom, offentlig konstruktør (public Post() { }). Dette gjør at jeg inne i metoden har lov til å skrive T item = new T(); for å instansiere nye objekter. Det betyr simpelthen at klassen må kunne opprettes uten å sende inn noen parametere.

Hvordan fungerer det i praksis:

1. Jeg sender inn typen `Post` som type-argument.
2. C# sjekker begrensningene:
    - Er `Post` en `ContentItem`? Ja.
    - Har `Post` en tom konstruktør `new()`? Ja.
3. Metoden kjører, oppretter instanser av `Post`, fyller dem ut og returnerer en `List<Post>`.
4. Variabelen `posts` blir automatisk av typen `List<Post>`. Du får full autokomplettering (IntelliSense) på alle feltene til `Post` i resten av koden din.

På denne måten kan jeg bruke samme metode for å laste inn både innlegg og sider, uten å måtte lage separate metoder for hver type:

```csharp
var posts = LoadContent<Post>(siteConfig, siteConfig.PostFolder);
var pages = LoadContent<Page>(siteConfig, siteConfig.PageFolder);
```

Men jeg kan også ha både innlegg og sider i samme liste, og filtrere ut enkelte elementer ved behov.

~~Også kan jeg ha if-blokker som sjekker typen, og gjøre spesifikk logikk for `Post` eller `Page` der det trengs.~~

> Å ha `is of` og `if` basert på typen i en generisk metode motvirker hele poenget med generics. Det lærte jeg rett etterpå. For hver gang jeg legger til en ny type, må jeg inn i generics for å endre.
> 
> Bruk heller polymorfisme, og la subklassene håndtere sin egen logikk. Da slipper du å endre generics hver gang du legger til en ny type.

Nå slipper jeg i alle fall å bruke mappenavn for å bestemme typen, og jeg kan håndtere både innlegg og sider i samme generator.

## Kompilering vs kjøring

En liten påminnelse om forskjellen mellom kompilering og kjøring når det kommer til typer og variabler;

Jeg forsøkte nemlig først å bruke `var type = contentItem.GetType().Name;` opp mot `Deserialize<type>(fileContent);`.

Men `type` er en variabel som kun eksisterer når programmet kjører. Den er ikke kjent når koden kompileres, og derfor kan ikke C# bruke den som type-parameter i `Deserialize<type>()`.

## Laste konfigurasjonen via metode på SiteConfig-klassen

Jeg flyttet `LoadConfig()` fra `Program.cs` til `SiteConfig.cs`, og endret den til en statisk metode som returnerer et `SiteConfig`-objekt. Dette gjør at jeg kan laste konfigurasjonen på en mer ryddig måte, og slipper å ha statiske variabler i `Program.cs`.

```csharp
public class SiteConfig
{
    public string ContentPath { get; set; } = "content";
    public string OutputPath { get; set; } = "output";
    public string PostFolder { get; set; } = "posts";
    public string PageFolder { get; set; } = "pages";
    public string TemplatesPath { get; set; } = "templates";
    public string SiteUrl { get; set; } = string.Empty;
    public string BasePath { get; set; } = string.Empty;

    public static SiteConfig Load()
    {
    //...
```

Så laster jeg den inn i `Program.cs` med `var siteConfig = SiteConfig.Load();`.

## Godt på vei med del 4

Med dette, så er jeg godt på vei med å løse oppgavene i del 4 av fase 1. Jeg har bedre strukturert koden, og gjort den mer fleksibel og robust.

_Struktur_
- [ ] Dele `Program.cs` i tydelige steg: 
	- [ ] Last konfigurasjon
	- [ ] Les innhold
	- [ ] Lag HTML
	- [ ] Skriv filer
- [x] Skille _innsamling_ fra _skriving_: først alle `ContentItem`-objekter i lister, deretter sidene
- [x] Flytte `LoadConfig` til `SiteConfig.Load()` eller en egen klasse
- [x] Flytte `Slugify` til `Utilities/`
- [x] Én felles metode for å lese både sider og innlegg

_Rydding_
- [ ] Konsekvente namespaces, tilgangsmodifikatorer og `static` der det passer
- [ ] Fjerne død kode og utkommentert kode
- [ ] Gå gjennom navn og kommentarer
- [x] Rydde opp i `BasePath` etter at konfigurasjonen er lest

_Publisering_
- [x] Bruke `BasePath` i alle lenker i malene
- [x] Arbeidsflyt i GitHub Actions som bygger og publiserer
- [x] Slå på GitHub Pages med Actions som kilde

Jeg lærte forøvrig også at det har svært liten betydning om man velger `""` eller `string.Empty`. Jeg synes sistnevnte er bedre, for det viser tydeligere at det er et aktivt valg, og ikke at man har glemt å sette inn tekst.

Når det kommer til et kontekst-objekt for SiteConfig, så har jeg foreløpig valgt å ikke implementere det. Jeg ser ikke behovet for det akkurat nå, men det kan være aktuelt senere.