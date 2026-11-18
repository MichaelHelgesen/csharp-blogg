---
title: Konstruktør vs objektinitialiserer
published: 2026-11-17
tags: [csharp, SSG-prosjekt]
---
**I dag lærte jeg om konstruktører, og hvorfor de er bedre enn objektinitialisering.**

I forrige innlegg laget jeg en `SiteContent`-klasse som speiler nettssidens innhold og struktur. Den ble laget med tomme felter for `SiteConfig` , poster, sider, og ikke minst tag-er.

Nederst i klassen laget jeg en metode `SetTags()` som skulle gå gjennom alle postene og hente ut alle unike tag-er, og legge dem i `AllTags`-feltet. 

Det fungerte, men det krevde at jeg først måtte opprette objektet, importere alle postene og sidene, og så kalle `SetTags()` for å fylle ut tag-listen. Da fikk jeg samme magefølelse som omtalt i forrige innlegg. Det føltes feil:

```csharp
var siteContent = new SiteContent
{
    SiteConfig = siteConfig,
    Posts = posts,
    Pages = pages
};

siteContent.SetTags();
```

Her er det egentlig tre ting som skjer. Det første er at objektet instansieres, det andre er at postene og sidene settes inn, og det tredje er at tag-listen fylles ut. Og alt er avhengig av at det skjer i riktig rekkefølge. Det er skjør kode, og kalles "temporal coupling": 

> Temporal coupling
> Kode som krever en bestemt rekkefølge for å fungere, men som ikke tvinger frem den rekkefølgen. Et objekt bør ideelt sett være ferdig når det instansieres.

I dette tilfelleter er det lett å glemme å kalle `SetTags()`, injisere poster eller sider eller jeg kan komme til å bruke objektet før det er ferdig. Koden ser da riktig ut, men nettsiden viser en tom tag-liste, og ingen feilmelding forteller hvorfor.

Jeg lærte jo nylig om "information expert"-prinsippet, og det er nettopp det som gjelder her. Poster, sider og tag-er hører hjemme i `SiteContent`, og alt som har med dette innholdet å gjøre, bør også helst håndteres der.

Derfor er det bedre å garantere at disse elementene er på plass i det øyeblikket objektet opprettes, og det gjøres med en konstruktør. Den må ha samme navn som klassen, og defineres med de parameterne som trengs for å lage objektet. I dette tilfellet er det `SiteConfig`, poster og sider:

```csharp
public SiteContent(SiteConfig siteConfig, List<Post> posts, List<Page> pages)
{
    SiteConfig = siteConfig;
    Posts = posts;
    Pages = pages;
}
```

Det som er ekstra smart, er at jeg i denne konstruktøren kan fylle ut tag-listen samtidig! Dermed slipper jeg å lage og kalle en egen metode til dette formålet:

```csharp
public SiteContent(SiteConfig siteConfig, List<Post> posts, List<Page> pages)
{
    SiteConfig = siteConfig;
    Posts = posts;
    Pages = pages;
    AllTags = [.. posts.SelectMany(p => p.Tags).Distinct()];
}
```

Poster og sider tvinges jo inn via konstruktøren, så på denne måten er det umulig å glemme å fylle ut tag-listen. Den blir alltid korrekt. 

Initialiseringen av `SiteContent`-objektet blir da mye enklere, Jeg slipper å kalle en egen metode, samt å injisere poster og sider med krøllparenteser, som kalles objektinitialisering.

```csharp
/* Gammel metode med objektinitialisering
// og kall til SetTags() som må gjøres i riktig rekkefølge.
var siteContent = new SiteContent
{
    SiteConfig = siteConfig,
    Posts = posts,
    Pages = pages
};

siteContent.SetTags();
*/

// Ny metode
var siteContent = new SiteContent(siteConfig, posts, pages);
```

Alt i alt mindre og bedre kode!