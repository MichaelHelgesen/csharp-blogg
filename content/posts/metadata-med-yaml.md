---
title: Metadata med YAML
---

# Metadata med YAML

**YAML er på plass, så nå kan jeg legge til metadata i tekstene for senere bruk**

Metadata er viktig å ha med i en blogg, men den skal som regel ikke synes blant det øvrige innholdet. Markdown-teksten og YAML-informasjonen må derfor separares før HTML-sidene genereres. Det kan både [Markdig](https://xoofx.github.io/markdig/) og [YamlDotNet](https://github.com/aaubry/YamlDotNet) hjelpe meg med.

## Finne YAML

Første steg er å lokalisere YAML-dataene. Det krever en spesiell metode i Markdig, så jeg må opprette en spesifikk datapipeline:

```csharp

``` 

YamlDotNet fungerer som en deserialisering. Det vil si at den konverterer flat data til et programobjekt.
 
> #### Begreper
>
> - Parsing: Fra tekst/rådata til programforståelig struktur (ofte for å lese den).
> - Serialisering: Fra programobjekt til lagringsvennlig format (tekst/filer).
> - Deserialisering: Fra lagringsvennlig format (tekst/filer) tilbake til programobjekt.

