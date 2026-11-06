---
title: Metadata med YAML
---

# Metadata med YAML

**YAML er på plass, så nå kan jeg legge til metadata i tekstene for senere bruk**

*Dato: 5. nov. 2026*

Metadata er viktig å ha med i en blogg, men den skal som regel ikke synes blant det øvrige innholdet. Markdown-teksten og YAML-informasjonen må derfor separares før HTML-sidene genereres. Det kan både [Markdig](https://xoofx.github.io/markdig/) og [YamlDotNet](https://github.com/aaubry/YamlDotNet) hjelpe meg med.

## Finne YAML

Første steg er å lokalisere YAML-dataene. Det krever en spesiell metode i Markdig, så jeg må opprette en spesifikk datapipeline:

```csharp
var markdownPipeline = new MarkdownPipelineBuilder()
    .UseYamlFrontMatter() // Hente ut YAML
    .Build(); // Bygge rørgaten.
``` 
I steden for å bruke den vanlige `Markdown.ToHtml()`-metoden, må jeg nå bruke `Markdown.Parse()`-metoden. Den returnerer et `MarkdownDocument`-objekt som inneholder både YAML og Markdown.
`mardownPipeline`-objektet må sendes med som parameter i begge metodene:

```csharp
var fileAsString = File.ReadAllText(file.FullName); // Leser filen
var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.
var html = Markdown.ToHtml(markdownTree, markdownPipeline); // Til HTML
var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
```

Resultatet er et `MarkdownDocument`-objekt som inneholder både YAML og Markdown. YAML-dataene kan hentes ut med `markdownTree.Descendants<YamlFrontMatterBlock>()`. Det returnerer en liste med alle YAML-blokkene i dokumentet. 

Markdig sitt YamlFrontMatter-tillegg støtter kun én YAML-blokk per dokument (den ser etter `---` på linje 1), så jeg kan hente den ut med `FirstOrDefault()`.

## Ny Post-klasse

For å kunne bruke YAML-dataene i programmet, må jeg opprette et nytt objekt som matcher strukturen i YAML-en. Jeg oppretter en ny klasse `Post` med de feltene jeg ønsker å hente ut:
```csharp
public class Post
{
    public string Title { get; set; } = "";
    public string Html { get; set; } = "";
}
```

Hvorvidt feltene skal være nullable, påkrevd eller forhåndsdefinert har jeg ikke tatt stilling til enda. Filen ble i alle fall opprettet under `/Models`, hvor det vil komme flere klasser etterhvert.

## Instansiering av Post med YamlDotNet

YamlDotNet fungerer som en deserialisering. Det vil si at den konverterer flat data til et programobjekt, i dette tilfellet `Post`. 

Jeg oppretter en ny instans av `DeserializerBuilder` med hjelpeklasser som gjør at den er case-insensitiv, så små og store bokstaver vil ikke ha noe å si. Den ignorerer også eventuelle manglende felter i klassen som matches mot:

```csharp
var yamlDeserializer = new DeserializerBuilder()
    .WithCaseInsensitivePropertyMatching() // Ignorer små og store bokstaver
    .IgnoreUnmatchedProperties() // Ignorer manglende felter i klassen som matches mot
    .Build();
```
 
> #### Begreper
>
> - Parsing: Analysere tekst/rådata til programforståelig struktur (ofte for å lese den).
> - Serialisering: Konvertere fra programobjekt til lagringsvennlig format (tekst/filer).
> - Deserialisering: Gjenopprette fra lagringsvennlig format (tekst/filer) tilbake til programobjekt.

Deretter gjennomfører jeg den faktiske deserialiseringen med `yamlDeserializer.Deserialize<Post>(yaml)`, hvor `yaml` er en streng som inneholder YAML-dataene. Resultatet blir et nytt `Post`-objekt med de verdiene som er definert i YAML-en.

`yaml`-strengen matches altså mot `Post`-klassen, så `title`-feltet i YAML-en vil bli matchet mot `Title`-feltet i `Post`-klassen.

```csharp
if (yamlBlock != null)
    {
        var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
        post = yamlDeserializer.Deserialize<Post>(yaml); // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
    }
```

Blokken for filkonverteringen ser nå slik ut:
```csharp
{
    var fileAsString = File.ReadAllText(file.FullName); // Leser filen
    var markdownTree = Markdown.Parse(fileAsString, markdownPipeline); // Deler inn dokumentet i YAML og MD.
    var html = Markdown.ToHtml(markdownTree, markdownPipeline); // Til HTML

    var post = new Post();

    var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.

    if (yamlBlock != null)
    {
        var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
        post = yamlDeserializer.Deserialize<Post>(yaml) ?? post; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
    }

    post.Html = html;

    // Skriv til HTML
    File.WriteAllText(Path.Combine(outputPath, Path.ChangeExtension(file.Name, "html")), html);
    
    Console.WriteLine($"File {file.Name} generated");
}
```

Siden yamlDeserializer kan returnere null hvis YAML-en er tom, må jeg legge til en null-coalescing operator (`??`) for å hindre at `post` blir null. Hvis YAML-en er tom, vil `post` beholde sin opprinnelige verdi.

Til slutt injiserte jeg HTML-en til Post-objektet i `post.Html`.

## Problemer med debug i Visual Studio

Da jeg prøvde å debugge programmet i Visual Studio, opplevde jeg at filbanene ikke stemte. 

Dette skyldtes at arbeidskatalogen i Visual Studio ikke nødvendigvis er den samme som prosjektmappen, noe som kan føre til at relative filbaner ikke fungerer som forventet.

I koden hadde jeg følgende:

```csharp
string outputPath = "../../output";
string sourcePath = "../../content/posts";
```

Disse er relative til stedet programmet startes fra, så hvis det ikke stemmer, må du justere stiene eller bruke absolutte stier.

Jeg oppdaterte launch.json-filen i Visual Studio for å sette arbeidskatalogen til prosjektmappen:

```json
{
  "profiles": {
    "CsharpBlogg.Generator": {
      "commandName": "Project",
      "workingDirectory": "$(ProjectDir)..\\..\\"
    }
  }
}
```

Da kunne jeg justere filbanene til:

```csharp
string outputPath = "output";
string sourcePath = "content/posts";
```

I terminalen er det en selv som bestemmer arbeidskatalogen, ved å stå i rotmappen når man kjører kommandoen:

```bash
dotnet run --project src/CsharpBlogg.Generator
```     

`--project` endrer ikke arbeidsmappen. Programmet starter fortsatt i mappen du står i. Det er nettopp derfor det fungerer: du står i rotmappen, peker på prosjektet med `--project`, og programmet finner content/posts relativt til der du står.

## Lært

Jeg brukte en del tid på dette steget, men det var verdt det. Nå kan jeg legge til metadata i tekstene mine, og hente dem ut i programmet. 

Jeg kan også bruke YAML til å definere andre ting, som for eksempel tags, kategorier, forfattere, osv. Det gir meg mye fleksibilitet og kontroll over innholdet mitt.

I tillegg har jeg lært:

- Hvordan bruke Markdig til å parse Markdown og YAML.
- Hvordan angi arbeidskatalogen i Visual Studio for å unngå problemer med relative filbaner.
- Operatoren "null-coalescing operator" (`??`) brukes for å håndtere null-verdier (hvis null så bruk oppgitt standardverdi bak operatoren).

## Hva som gjenstår i del 2:

- URL-vennlige titler på filer.
- Flytte stiene til mappene til en konfigurasjonsfil og lese den inn ved oppstart.
- Håndtere at konfigurasjonsfilen mangler eller har feil.