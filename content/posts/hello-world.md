---
title: Dette er en test
published: 2026-11-05
---

# Hallo verden!

*Dette er mitt første blogginnlegg i det som forhåpentligvis skal bli min nye nettside basert på egen «[Static Site Generator](https://en.wikipedia.org/wiki/Static_site_generator)», programmert i C#.*

_5. november 2026_

Mange vil kanskje kalle det naivt å programmere noe «for hånd» i disse KI-dager, men det er en ypperlig måte å lære et programmeringsspråk på. 

Å kombinere programmeringsprosjekter med noe man synes er gøy er en god ide, fremfor å tvinge seg halvvegs gjennom tørre instruksjonsvideoer og risikere å bli sittende fast i «tutorial hell».

Sant nok pløyer jeg meg gjennom «[The C# Academy](https://thecsharpacademy.com/)» og «[The C# Players Handbook](https://csharpplayersguide.com/)», to ypperlige kilder som kommer høyt anbefalt, men det er liksom noe eksta inspirerende ved å utvikle noe eget.

Ekstra gøy er det å kunne ta programmet i bruk umiddelbart, slik som nå. Denne nettsiden vil slik sett fungere like mye som en logg eller journal, i tillegg til den mer tradisjonelle bloggen.

Jeg lærte faktisk nettopp i dag om uttrykket "dogfooding", fra engelsk _«eating your own dog food»_. Det ble kjent i programvarebransjen på slutten av 1980-tallet, da en leder hos Microsoft brukte det i en intern e-post for å oppfordre de ansatte til å bruke sine egne produkter i hverdagen. Ideen er enkel: hvis produktet ikke er godt nok for deg selv, er det ikke godt nok for andre heller.


## Formål

Ønsket er å lage en nettsidegenerator på lik linje med min eksisterende [HUGO-løsning](https://gohugo.io/), og jeg ønsker i første omgang følgende:

- Kunne opprette et bloggdokument fra hvor som helst i terminalen via en kommando.
- Gå inn i dokumentet ved opprettelse, for å starte skrivingen umiddelbart.
- Ved opprettelse settes tittel samt noe standard metadata som dato.
- Dokumenter opprettes som markdown.
- Metadata skrives med YAML.
- Katalogstruktur definerer nettsidens oppbygning.
- Støtte for bilder, kodevisning, video med mer.
- HTML-maler.

## Teck stack

Jeg skal ikke starte helt på bar bakke, for det kan holde meg tilbake og gjøre så jeg stagnerer. Prosjektet ble derfor opprettet med følgende plugins:

- [Markdig](https://xoofx.github.io/markdig/): Konvertere MarkDown til HTML.
- [YamlDotNet](https://github.com/aaubry/YamlDotNet): Analysere YAML.

### Struktur

HUGO er inspirasjonen, så jeg laget følgende struktur på prosjektet:

```text
Csharpblogg/
├── src/Csharpblogg.Generator/   # C#-koden
├── content/
│   ├── posts/               # blogginnlegg (.md)
│   └── pages/               # faste sider, f.eks. om-meg.md
├── templates/               # layout.html, post.html, index.html
├── static/                  # CSS, bilder
└── output/                  # generert nettside
```

### Foreløpig kode

Etter en liten time hadde jeg et utgangspunkt som fungerte:

```csharp
using Markdig;

string outputPath = "../../output";
string sourcePath = "../../content/posts";

// Filene å gå gjennom. EnumerationOptions er "hvordan skal filene gjennomgås".
// Her så skal alle filer matche store og små bokstaver i "md".

if (!Directory.Exists(sourcePath))
{
    Console.WriteLine($"Kildemappen ({sourcePath}) mangler eller er ikke tilgjengelig");
    return;
}

var files = GetFiles(sourcePath, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

try
{
    // Prøv å opprette katalog
    Directory.CreateDirectory(outputPath);
}
catch (UnauthorizedAccessException e)
{
    Console.WriteLine($"Ingen tilgang til å opprette {outputPath} - {e}");
    return;
}

// Iterere over filer
foreach (var file in files)
{
    try
    {
        var markdown = File.ReadAllText(file.FullName);
        var html = Markdown.ToHtml(markdown);
        File.WriteAllText(Path.Combine(outputPath, Path.ChangeExtension(file.Name, "html")), html);
        Console.WriteLine($"File {file.Name} generated");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting and creating file {file.FullName} - {ex}");
    }
}

FileInfo[] GetFiles(string path, string pattern, EnumerationOptions options)
{
    return new DirectoryInfo(path).GetFiles(pattern, options);
}
```

Allerede her var det mye å lære.

- Hente filer med `GetFiles`.
- Konvertere til MarkDown via `Markdigs Markdown.ToHtml`.
- Lese og konvertere systemfil til string med `ReadAllText`.
- Utvidet `GetFiles` med parametere `path`, `searchPattern`, og `EnumerationOptions`.
    - Lærte at Enumeration betyr "å gå gjennom noe, en etter en". Options er altså hva denne gjennomgangen skal ta hensyn til. Både store og små bokstaver i dette tilfellet.
- Sjekk etter katalog med `Directory.Exists`.
- Opprette katalog med `CreateDirectory`.
- Skrive til fil med `File.WriteAllText(Path.Combine(outputPath, Path.ChangeExtension(file.Name, "html")), html);`
- Bruke `Path.Combine` og `Path.ChangeExtension`.

## Steg to

Planen videre innebærer å utvide med følgende:

- URL-vennlige titler på filer
- Flytte stiene til mappene til en konfigurasjonsfil og lese den inn ved oppstart
- Håndtere at konfigurasjonsfilen mangler eller har feil
- Lage en klasse eller record for innlegg (Post) med tittel, dato, tags og HTML
- Skille frontmatter fra resten av Markdown-filen
- Lese frontmatter med YamlDotNet og fylle Post
- Sørge for at YAML-blokken ikke havner i HTML-en
