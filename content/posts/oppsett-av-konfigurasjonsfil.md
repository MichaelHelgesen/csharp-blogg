---
title: Oppsett av konfigurasjonsfil
---

# Opprettelse av konfigurasjonsfil

*Dato: 6. nov. 26*

**Å ha hardkodede filbaner i programkoden er sårbart. Derfor ville jeg at min statiske sidebygger skulle ha en konfigurasjonsfil**

Jeg besluttet at konfigurasjonsfilen på rot skulle være JSON-format med navnet `config.json`, mens konfig-klassen fikk navnet `SiteConfig` og ble lagt i `Models`-mappen.

Først definerte jeg feltene i klassen:

```csharp
namespace CsharpBlogg.Generator.Models;

public class SiteConfig
{
    public string ContentPath { get; set; } = "content/posts";
    public string OutputPath { get; set; } = "output";
}
```

Denne klassen skal instansieres under programmets oppstart, og enten danne grunnlag for opprettelsen av `config.json`, eller importere verdiene fra `config.json`, dersom den allerede eksisterer. 

Formålet er å la brukeren definere egne innstillinger i JSON-filen, som vil påvirke programmets kjøring. Den lar et program avvike fra standarden uten at koden endres.

## Oppsett

Jeg valgte å samle denne logikken i en egen metode som jeg kalte `LoadConfig`.

Først laget jeg en variabel for konfigfilens navn, og opprettet en ny instans av konfig-klassen:

```csharp
static SiteConfig LoadConfig()
{
    var jsonConfigFilePath = "siteconfig.json"; // Banen til konfigurasjonsfilen
    var siteConfig = new SiteConfig(); // Nytt SiteConfig-objekt
```

Først sjekker jeg om konfig-filen eksisterer med `File.Exists`. Deretter prøver jeg å lese innholdet og deserialisere den til `SiteConfig`-objektet.

Er resultatet `null` så sørger jeg for tilbakestilling til det opprettede `SiteConfig`-objektet.

For å fange feil, la jeg til flere ulike `catch`-blokker. Konvensjonen er å gå fra den mest aktuelle feilmeldingen, til den mest generelle. Så først JSON-feil, deretter filfeil, adgangsfeil og til slutt generell.

```csharp 
    if (File.Exists(jsonConfigFilePath))
    {
        try
        {
            var jsonContent = File.ReadAllText(jsonConfigFilePath);
            siteConfig = JsonSerializer.Deserialize<SiteConfig>(jsonContent) ?? siteConfig;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing {jsonConfigFilePath}: {ex.Message}");
            throw;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading {jsonConfigFilePath}: {ex.Message}");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"No access to {jsonConfigFilePath}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error with {jsonConfigFilePath}: {ex.Message}");
            throw;
        }
    }
```

Hvis filen ikke finnes prøver jeg å opprette den, ved å serialisere `SiteConfig`-klassens standardverdier over i en JSON-fil.

I denne blokken kaster jeg ikke en avslutningsfeil, for hvis ikke JSON-filen skapes fungerer det likevel siden `SiteConfig`-objektet uansett opprettes med standardverdier.

Til slutt returnerer jeg dette objektet.

```csharp
    else
    {
        try
        {
            Console.WriteLine($"Missing {jsonConfigFilePath}, attempting to create one with default values.");
            File.WriteAllText(jsonConfigFilePath, JsonSerializer.Serialize(siteConfig, new JsonSerializerOptions { WriteIndented = true })); // writeIndented = true som gir penere formatering.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not create {jsonConfigFilePath}: {ex.Message}");
        }
    }
    return siteConfig;
}
```

Tidlig i `Program.cs` kaller jeg denne metoden i en `try`-blokk, for å vise eventuelle feil og avslutte, dersom det kastes fra `LoadConfig`-metoden:

```csharp
SiteConfig siteConfig;

// Prøv å lese konfigurasjonsfilen
try
{
    siteConfig = LoadConfig();
}
catch // Beskjed kastes fra metoden.
{
    return;
}
```

Det ser ut til å fungere ganske så bra!

## «Top level statements»

I metoden `LoadConfig` hadde jeg først ikke `var siteConfig = new SiteConfig();` Da fikk jeg en feil i koden som kalte metoden: «use of unasigned local variable». 

I `Program.cs` uten Main-metode, det som kalles top-level statements, er `LoadConfig` en lokal funksjon. Lokale funksjoner kan se og bruke variabler fra koden rundt seg. Hvis `LoadConfig` bruker navnet `siteConfig` uten å deklarere sin egen variabel, for eksempel med en linje som `siteConfig =` ... uten `var` foran, så bruker den variabelen fra `Program.cs`.

Og den variabelen har ingen verdi ennå når *`LoadConfig` kalles. Den får jo verdien sin fra det `LoadConfig` returnerer, så en tom variabel går bare rundt og rundt. 

Kompilatoren ser dette og melder feilen der metoden kalles.