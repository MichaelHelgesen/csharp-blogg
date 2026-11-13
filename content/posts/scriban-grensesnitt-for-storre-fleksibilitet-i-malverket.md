---
title: "Scriban-grensesnitt for større fleksibilitet i malverket"
published: 2026-11-13
tags: ["Scriban", "SSG-prosjekt"]
---

**Scriban har funksjoner langt utover det å bruke enkle variabler i HTML-filer. Med `TemplateLoader` kan man for eksempel laste inn og bruke maler på en mer fleksibel måte.**

Å bygge maler for statiske nettsider vil fort kreve et ønske om å gjenbruke deler av malene, samt å implementere dem i hverandre, som dette:

```html
// default.html
{{ header }}
<div>
    {{ content }}
</div>
{{ footer }}

// header.html
<header>
	<h1>
		<a href="{{ basepath }}">My homepage</a>
	</h1>
</header>
```

Først gjorde jeg det på den grunnleggende måten, ved å lese, parse og rendere hver mal for seg, og deretter sette resultatene inn i hovedmalen. Det fungerte, men var ikke særlig elegant:

```csharp
var defaultTemplate = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/default.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
var headerTemplate = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/header.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
var footerTemplate = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/footer.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

if (defaultTemplate != null)
{
    
    string footer = File.ReadAllText(footerTemplate.FullName);
    var footerTemplateParsed = Template.Parse(footer);
    var footerRendered = footerTemplateParsed.Render(new { basepath = siteConfig.BasePath });   

    string header = File.ReadAllText(headerTemplate.FullName);
    var headerTemplateParsed = Template.Parse(header);
    var headerRendered = headerTemplateParsed.Render(new { basepath = siteConfig.BasePath });
    
    string content = File.ReadAllText(defaultTemplate.FullName);
    var template = Template.Parse(content);
    html += template.Render(new { header = headerRendered, footer = footerRendered, content = content });
}
```

## TemplateLoader

Scriban har heldigvis et grensesnitt som gjør det mulig å laste inn maler på en mer fleksibel måte, ved å bruke `TemplateLoader`. 

Det abstraherer bort hvordan malene lastes inn, og gjør det mulig å bruke `include`-direktivet i malene:

```html
{{ include "general/header.html" }}

// Fremfor
{{ header }}
```

Det som skjer i Scribans run-time er at `TemplateLoader` bygger en sti til malen som skal lastes inn, og deretter laster den inn fra kilden.

Det krever at man først implementerer følgende klasse:

```csharp
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;
/// <summary>
/// A simple ITemplateLoader loading templates from a configured directory.
/// </summary>
public class MyIncludeFromDisk : ITemplateLoader
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private readonly string _templateRoot;

    public MyIncludeFromDisk(string templateRoot)
    {
        _templateRoot = Path.GetFullPath(templateRoot);

        if (!_templateRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            _templateRoot += Path.DirectorySeparatorChar;
        }
    }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        var templatePath = Path.GetFullPath(Path.Combine(_templateRoot, templateName));

        if (!templatePath.StartsWith(_templateRoot, PathComparison))
        {
            throw new ScriptRuntimeException(callerSpan, $"Include `{templateName}` is outside the template root.");
        }

        return templatePath;
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        // Template path was produced by the `GetPath` method above in case the Template has 
        // not been loaded yet
        return File.ReadAllText(templatePath);
    }
    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return new ValueTask<string>(File.ReadAllTextAsync(templatePath));
    }
}
```

Hva er det som skjer her? La oss gå gjennom linje for linje:

### Definere klassen

```csharp
public class MyIncludeFromDisk : ITemplateLoader
```

Her arver vi grensesnittet `ITemplateLoader`, som kreves for å kunne laste inn maler fra ønsket kilde.

Det `ITemplateLoader` pålegger oss, er å implementere tre metoder: `GetPath`, `Load` og `LoadAsync`. Det er disse metodene som kalles av Scriban når den støter på et `include`-direktiv i en mal.

Men først møter vi noen støttemetoder og variabler som hjelper oss med å implementere disse metodene.

### Bestemme hvordan vi sammenligner filstier

```csharp
private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
```

Deretter definerer vi en statisk readonly variabel som bestemmer hvordan vi skal sammenligne filstier, avhengig av operativsystemet. 

På Windows er filstier ikke case-sensitive, mens de er det på andre operativsystemer.

### Definere rotmappen for malene

```csharp
 private readonly string _templateRoot;
 ```

 Så følger en variabel som holder på rotmappen for malene vi ønsker å laste inn. Verdien settes i konstruktøren:

 ### Konstruktør

 ```csharp
 public MyIncludeFromDisk(string templateRoot)
    {
        _templateRoot = Path.GetFullPath(templateRoot);

        if (!_templateRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            _templateRoot += Path.DirectorySeparatorChar;
        }
    }
```

I denne konstruktøren tar vi inn en sti til rotmappen for malene, og sørger for at den er en fullstendig sti. Vi legger også til et skråstrek på slutten av stien dersom det ikke allerede er der.

### Bygge stien til malen

```csharp
public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        var templatePath = Path.GetFullPath(Path.Combine(_templateRoot, templateName));

        if (!templatePath.StartsWith(_templateRoot, PathComparison))
        {
            throw new ScriptRuntimeException(callerSpan, $"Include `{templateName}` is outside the template root.");
        }

        return templatePath;
    }
```

Dernest implementerer vi metoden `GetPath` (den første av de tre påtvungne metodene), som tar inn konteksten, kilden for kallet (hvor i koden malen ble kalt, som et bokmerke) og navnet på malen vi ønsker å laste inn. Vi kombinerer rotmappen med navnet på malen for å få en fullstendig sti til malen. 

Vi sjekker også at malen ligger innenfor rotmappen og kaster et unntak hvis ikke. Dette er av sikkerhetshensyn, for å unngå at man utilsiktet laster inn filer utenfor det som er ment å være tilgjengelig.

### Laste inn malen

```csharp
public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        // Template path was produced by the `GetPath` method above in case the Template has 
        // not been loaded yet
        return File.ReadAllText(templatePath);
    }
```

Nest sist implementerer vi metoden `Load`, som tar inn konteksten, kilden og stien til malen vi ønsker å laste inn. 

SourceSpan-parameteren brukes til å gi mer informasjon om hvor i koden malen ble lastet inn, men vi bruker den ikke her.

Vi leser deretter innholdet i filen og returnerer det som en streng.

### Laste inn malen asynkront

```csharp
public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return new ValueTask<string>(File.ReadAllTextAsync(templatePath));
    }
```

Til sist implementerer vi en asynkron versjon av `Load`-metoden, som gjør det mulig å laste inn maler på en ikke-blokkerende måte. 

Man kan velge å bruke enten den synkrone eller asynkrone versjonen avhengig av behovet.

Vi bruker `File.ReadAllTextAsync` for å lese inn filen asynkront, og returnerer resultatet som en `ValueTask<string>`.

`ValueTask` er et optimalisert alternativ til `Task` som kan brukes når resultatet allerede er tilgjengelig, og dermed unngår unødvendig overhead. Og her er den tilgjengelig fordi vi returnerer resultatet av `File.ReadAllTextAsync`, som allerede er en `Task<string>`.

## Endre koden som laster inn malene

Nå kan jeg endre koden som laster inn malene til dette:

```csharp
// 1. Opprett en instans av loaderen med rotmappen for malene
var templateLoader = new MyIncludeFromDisk(siteConfig.TemplatesPath);

// 2. Opprett en TemplateContext og fest loaderen til den
var context = new TemplateContext
{
    TemplateLoader = templateLoader
};

// 3. Opprett et ScriptObject med variablene som skal brukes i malene
var scriptObject = new ScriptObject
{
    { "basepath", siteConfig.BasePath },
    { "content", contentItem.Html }
};

// 4. Legg ScriptObject til i konteksten
context.PushGlobal(scriptObject);

// 5. Finn og les default.html fra malmappen
var defaultTemplate = new DirectoryInfo(siteConfig.TemplatesPath).GetFiles("general/default.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

// 6. Parse og render default.html med konteksten
if (defaultTemplate != null)
{
    string content = File.ReadAllText(defaultTemplate.FullName);
    var template = Template.Parse(content);
    html += template.Render(context);
}
```

Først initialiserer jeg en instans av `MyIncludeFromDisk` med banen til rotmappen for malene som argument. 

Deretter oppretter jeg en `TemplateContext` (konteksten for å bygge malene) og setter `TemplateLoader`-egenskapen til instansen initialisert over.

Så oppetter jeg et `ScriptObject` som inneholder variablene jeg ønsker å bruke i malene, og legger det til i konteksten. Å samle variabler og funksjoner som malene kan bruke er nettopp `ScriptObject` sin hensikt.

Deretter finner jeg hovedmalen `default.html`, leser innholdet, parser det og renderer det med konteksten.

Da vil alle `include`-direktivene i malen automatisk bruke `MyIncludeFromDisk` til å laste inn de nødvendige malene.

## Annet jeg har lært og gjort i denne omgangen

### Rydding

Jeg hadde følgende: 

```csharp
var posts = ContentReader.LoadContent<Post>(siteConfig, siteConfig.PostFolder);
var pages = ContentReader.LoadContent<Page>(siteConfig, siteConfig.PageFolder);
```

Her sender jeg siteConfig to ganger... Det er unødvendig.

La heller til følgende i `ContentReader`:

```csharp
string contentFolder = typeof(T).Name.ToLower() + "s"; // Hvilken type innhold er det? Page eller Post?
```

Men det er dumt igjen. For her kan det bli krøll med mappenavn, med å legge på "s" osv.

Bedre å legge det som `get`-felter i SiteConfig:

```csharp
    public string PostFolderPath => Path.Combine(ContentPath, PostFolder);
    public string PageFolderPath => Path.Combine(ContentPath, PageFolder);
```

Da kan jeg i `Program.cs` gjøre følgende:

```csharp
var posts = ContentReader.LoadContent<Post>(siteConfig.PostFolderPath);
var pages = ContentReader.LoadContent<Page>(siteConfig.PageFolderPath);
```

### Logikk i oppbygningen av malene

Etter tenkning og dialog med meg selv, har jeg kommet frem til følgende prioriteringsliste for hvordan malene skal velges:

1. Sjekk YAML etter spesialmal.
2. Sjekk mappe og filnavn av markdown mot mappe og filnavn i templates.
3. Dersom ikke match i punkt 2 bruk typemalen, så side får malen i for eksempel `/pages/_default.html`, og innlegg: `/post/_default.html` (prefiks for å unngå ev. treff mot filnavn).
4. Dersom ingen match på type-folder, bruk den mest grunnleggende malen i `/general`.

### Diverse

- Lærte at `return` kan brukes med 1 for feil, og 0 for suksess.
- La inn mer inline stil.
- Flyttet ContentConverter metoden til egen fil under `/Services`.
- Flyttet lagring av filer til egen SaveFiles-metode i egen fil under `/Services`.
- Det er øverste nivå i koden som skal ta seg av feilbeskjeder og stoppingen av programmet. Metodene skal helst ikke stoppe programmet:

```csharp
// Prøv å lese konfigurasjonsfilen
SiteConfig siteConfig;
try
{
    siteConfig = SiteConfig.Load();
}
catch (Exception e)
{
    Console.WriteLine($"Error loading site configuration: {e.Message}");
    return 1;
}
```

Med det er del 4 i fase 1 av prosjektet ferdig:

_Struktur_
- [x] Dele `Program.cs` i tydelige steg: 
	- [x] Last konfigurasjon
	- [x] Les innhold
	- [x] Lag HTML
	- [x] Skriv filer
- [x] Skille _innsamling_ fra _skriving_: først alle `ContentItem`-objekter i lister, deretter sidene
- [x] Flytte `LoadConfig` til `SiteConfig.Load()` eller en egen klasse
- [x] Flytte `Slugify` til `Utilities/`
- [x] Én felles metode for å lese både sider og innlegg

_Rydding_
- [x] Konsekvente namespaces, tilgangsmodifikatorer og `static` der det passer
- [x] Fjerne død kode og utkommentert kode
- [x] Gå gjennom navn og kommentarer
- [x] Rydde opp i `BasePath` etter at konfigurasjonen er lest

_Publisering_
- [x] Bruke `BasePath` i alle lenker i malene
- [x] Arbeidsflyt i GitHub Actions som bygger og publiserer
- [x] Slå på GitHub Pages med Actions som kilde
