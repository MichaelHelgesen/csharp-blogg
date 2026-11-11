---
title: Utvikle en slugify-metode for nettadresser
tags: [ssg-prosjekt, csharp]
slug: Dette er slug uten riktig format
published: 2026-11-07 
---

**Jeg utviklet en slug-metode for å generere URL-er til blogginnleggene i min statiske sidegenerator. Det krevde mer enn antatt**

*Dato: 7. november 2026* 
#ssg-prosjekt #csharp

Etter at oppgaven med å konvertere «markdown»-filer til HTML-filer var gjort, måtte jeg sørge for å gi dem gyldige og gode URL-er.

Det innebar å først ta noen vesentlige avgjørelser når det kommer til logikk, påkrevd informasjon og struktur.

Det første som måtte bestemmes var slug-ens grunnlag:

1. Skal slug genereres med utgangspunkt i tittel, filnavn og/eller være manuell?

Dernest: 

2. Hvis slug i metadata slettes, skal fallback være tittel eller filnavn? Eller rangeres mellom dem?
3. Hvordan lage slug og filnavn uten spesialtegn? 
4. Skal filnavn være uavhengig av URL?
5. Skal tittel angis i YAML, eller i dokumentet?
6. slug-er bør ikke endres med mindre det er gjennomtenkt. Hvordan håndtere det?
7. Hva med metatittel?

Det er helt sikkert andre ting også, som jeg foreløpig ikke har tenkt på, men punktene over var mer enn nok å bryne seg på i første omgang.

## Punkt 1: Utgangspunktet for slug

Min første tanke var at slug og filnavnet genereres ved opprettelsen av blogginnlegget, med tittelen som grunnlag. Det forutsetter at den opprettes med en kommando.

`new "Hei verden!"` vil slikt sett generere følgende filnavn og slug-felt: `hei-verden.md`, `hei-verden`.

Hvis filen opprettes manuelt, kan jeg i konverteringen bruke filnavnet som grunnlag.

Brukes kommandoen for å opprette nytt innlegg kan slug vises i metadataen. På den måten er det mulig å endre filnavnet på markdownfilen, uten å påvirke slug.

Ønsker man en annen slug kan den overskrives i slug-feltet.

## Punkt 2: Hva om slugfeltet slettes?

Jeg tenker at en tittel antagelig langt oftere vil være gjenstand for endring sammenlignet med filnavnet. Derfor fremstår det for meg som tryggere at sistnevnte skal være fallback dersom en slug tidligere er manuelt endret, og deretter slettes.

Det kan selvsagt skape stor ulikhet mellom tittel og slug, og det kan være vanskelig å se grunnlaget for resultatet, men jeg vurderer det foreløpig som et tryggere valg med tanke på at URL-er bør være så stabile som mulig. I alle fall etter å ha levd en stund.

Et alternativ er selvsagt å først gå til tittel, dersom den eksisterer, og til slutt filnavnet.

## Punkt 3: Hvordan generere en god slug

En slug skal strippes for spesialtegn, mellomrom med mer, og krevde en egen metode:

```csharp
static string slugConverter(string title)
{
    string titleCopy = title
            .ToLowerInvariant()
            .Replace("ø", "o").Replace("å", "aa").Replace("æ", "ae");

    titleCopy = titleCopy.Normalize(NormalizationForm.FormD);
    
    char[] arr = titleCopy.ToCharArray();

    arr = Array.FindAll(arr, c => 
        char.IsAsciiLetterOrDigit(c)
        || char.IsWhiteSpace(c)
        || c == '-'
    );

    titleCopy = new string(arr);
    titleCopy = Regex.Replace(titleCopy, @"[\s-]+", "-");
    titleCopy = titleCopy.Trim('-'); 
                                    
    return titleCopy;
```

Rekkefølgen ble som følger:

- Først til små bokstaver med `ToLowerInvariant`. Å velge denne er sikrere enn `ToLower`, som følger språkinnstillingene på maskinen. Noen språk konverterer nemlig ikke til små bokstaver med `ToLower`. 
- Deretter erstattes "ø,æ å" med `Replace`. Hvis jeg ikke gjorde det på dette nivået, ville "æ" og "ø" blitt fjernet via metoden `IsAsciiLetterOrDigit`. Den fjerner nemlig bokstaver fra andre enn det engelske alfabetet.
- Så brukte jeg `Normalize` som deler opp alle tegn med aksent. á blir ̛a.
- Strengen konverteres deretter til array med karakterer: `char[] arr = titleCopy.ToCharArray();`
- Arrayen itereres, og returnerer kun bokstaver, mellomrom og bindestrek.
- Så satt jeg array sammen til string igjen.
- Deretter en regex for å erstatte gjentagende mellomrom og bindestreker med én bindestrek, for å unngå for eksempel "  " og "----".
- Til slutt trimmer jeg begge ender for eventuelle bindestreker, og returnerer strengen.

Jeg jobbet ganske lenge med dette, for å få det til å bli riktig. Rekkefølgen påvirker, og hvilke metoder som er best vet jeg for lite om enda. Så resultatet er en blanding av info fra dokumentasjon, Stack Overflow og KI. 

Jeg foretrekker mindre regex, fordi regex er vanskelig å lese, og krever mer ressurser sammenlignet med en loop, ikke at det er så viktig i mitt tilfelle.

> #### Lært: 
> - `IsAsciiLetterOrDigit` må skje etter konvertering av "øæ", for ellers fjernes de helt. "å" regnes som aksent, så den splites med >`Normalize`.
> - `Trim` fjerner i begge ender, så unødvendig med `TrimStart` og >`TrimEnd` i mitt tilfelle for å fjerne bindestrek i begge ender.
> - At `char` har kategorier som kan sjekkes med `char.>GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.>NonSpacingMark`.

Resultatet ble:

| Tittel                 | Slug                |
| ---------------------- | ------------------- |
| `C# – en introduksjon` | `c-en-introduksjon` |
| `Hei -- verden`        | `hei-verden`        |
| `-Hei verden-`         | `hei-verden`        |
| `ÆØÅ-boka`             | `aeoaa-boka`        |
| `Café på hjørnet`      | `cafe-paa-hjornet`  |

## Punkt 4: Skal filnavn være uavhengig av slug

Det svarte jeg på i resonnementet under punkt 2. 

Jeg landet på at foreløpig skal slug i hovedsak følge filnavnet, med mindre den er manuelt satt i metadata.

## Punkt 5: Tittel som metadata, eller tekst

Begge deler fungerer, men kan ha ulike konsekvenser.

- **I metadata:** Tittelen er metadata, og malen lager `<h1>` ut fra den. Dokumentet inneholder bare selve teksten. Da er det også lett at tittelen kan brukes andre steder, som forsiden og i RSS.
- **Første `#` i dokumentet:** Mer naturlig når man skriver, fordi Markdown-filen ser ut som det ferdige innlegget. Den kan hentes fra syntakstreet, på samme måte som YAML-blokken. Markdig har en egen blokktype for overskrifter.

Ett felles mål for begge: unngå at tittelen vises to ganger, både fra malen og fra dokumentet.

I denne omgang velger jeg å etablere tittel i metadataen. Hovedgrunnen er for meg forestillingen om at det er større sjangs for feil hvis man skal lete etter en tittel i tekstfeltet. Den kan defineres med feil overskriftstype, glemmes, gjentas og så videre.

## Punkt 6: URL-er må være stabile

Dette har jeg også svart på, via de øvrige punktene.

Hvis jeg lager en kommando for å opprette nye dokumenter, vil tittelen være kilden til både slug og filnavn. 

- Dersom tittel endres, vil ikke slug endres. 
- Dersom slug angis manuelt, vil den endres.
- Dersom slug ikke er manuelt angitt/endret, og filnavnet endres, så vil slug oppdateres til å reflektere filnavnet.

Jeg må nok teste dette godt før jeg trekker endelige konklusjoner.

## Punkt 7: Hva med metatittel?

Den kan også speile tittel ved etablering. Deretter er det et manuelt felt. Dersom det er tomt, brukes tittel. Dersom tittel også mangler, blir ingen metatittel satt.

## Logikken ved konvertering til HTML

Det første som skjer er at jeg deserialiserer YAML-metadataen til en ny instans av `Post`-klassen. Det danner grunnlaget for videre behandling.

```csharp
// ...
var post = new Post();

var yamlBlock = markdownTree.Descendants<YamlFrontMatterBlock>().FirstOrDefault(); // Henter YAML-blokken, hvis den finnes.
if (yamlBlock != null)
{
    var yaml = yamlBlock.Lines.ToString(); // Henter rå YAML som streng
    post = yamlDeserializer.Deserialize<Post>(yaml) ?? post; // Instansierer et Post-objekt og angir feltene, eller henviser til originalobjektet hvis tom YAML.
}
//...
```

Målet er at slug skal defineres ved opprettelse av filen, men jeg har foreløpig ikke kommet så langt. I mellomtiden må jeg derfor gjøre det i koden, men legge til rette forfremtidig logikk:

```csharp
if (post.Slug == null || post.Slug == "")
{
    post.Slug = slugConverter(file.Name.Split(".")[0]);
}
else
{
    post.Slug = slugConverter(post.Slug);
}
``` 

Er den ikke satt eller en tom streng brukes filnavnet. Dersom den er satt, kjører vi metoden med eksisterende data, for å forsikre oss om at det blir en gyldig slug.

### Tittel og metatittel

Siden jeg har bestemt at tittel skal være en del av metadataen, må jeg injisere den i strengen som skal konverteres til HTML

```csharp
var html = Markdown.ToHtml($"# {post.Title}", markdownPipeline); // Tittel fra YAML
html += Markdown.ToHtml(markdownTree, markdownPipeline); // Tekst til HTML
```

Metadata er valgfri, og faller tilbake på hovedtittelen. Konfigurasjonen kan for eksempel legge til navnet på nettsiden etter den, som i “Hei verden | Min blogg”. Da trenger man nesten aldri skrive den selv.

```csharp
if (post.MetaTitle == null || post.MetaTitle == "")
{
    post.MetaTitle = post.Title;
}
```

### Tittel

Så måtte jeg tenke over tittelen: **hva skal skje hvis tittelen mangler i frontmatter?** 

- Hoppe over innlegget med en advarsel
- Lage en tittel fra filnavnet, for eksempel `hei-verden` til “Hei verden”
- Publisere uten tittel

Jeg tenker at det ikke skal være mulig å opprette en fil uten en tittel, så på den måten er det påkrevd, men i koden kan det per nå lages HTML, dersom tittel mangler.

Når en bruker kommer inn i filen, står tittel allerede angitt i YAML, og en slug generert basert på tittelen. Så kan brukeren velge å endre selv. Dersom brukeren tar bort tittel og slug, er det filnavnet som må brukes som slug, eller skal filen ikke bli konvertert. I CSM-er er ofte tittel et påkrevd felt. Eller så kan jeg gjøre det slik at markdown uten tittel automatisk settes som draft. Det er en del logikk å tenke gjennom her:

**Én kilde til sannhet for sluggen**  
Hvis `new` lager både et filnavn _og_ en `slug` i frontmatter fra samme tittel, har man samme informasjon to steder. Endres  den ene, hvilken gjelder? Et enklere alternativ:

- Filnavnet _er_ sluggen
- `slug` i frontmatter er valgfri, og bare for når du vil overstyre

Følgende konklusjon:

| Felt                 | Kilde                     | Mangler                                                         |
| -------------------- | ------------------------- | --------------------------------------------------------------- |
| Tittel               | Frontmatter               | Innlegget behandles som kladd, med advarsel                     |
| Slug og HTML-filnavn | `slug` i frontmatter      | Filnavnet på Markdown-filen                                     |
| Metatittel           | `metaTitle` i frontmatter | Tittelen                                                        |
| Kladd                | `draft` i frontmatter     | `false` som standard, og automatisk false dersom tittel mangler |


Med det, er alle oppgaver i del 2 av fase 1 fullført. Neste steg er å implementere maler, og koble dem til innholdet.

**Del 2 av fase 1 fullført!:**
- [x] URL-vennlige titler på filer
- [x] Flytte stiene til mappene til en konfigurasjonsfil og lese den inn ved oppstart
- [x] Håndtere at konfigurasjonsfilen mangler eller har feil
- [x] Lage en klasse eller record for innlegg (`Post`) med tittel, dato, tags og HTML
- [x] Skille frontmatter fra resten av Markdown-filen
- [x] Lese frontmatter med YamlDotNet og fylle `Post`
- [x] Sørge for at YAML-blokken ikke havner i HTML-en