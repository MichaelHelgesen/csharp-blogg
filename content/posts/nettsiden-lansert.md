---
published: 2026-11-10
title: Nettsiden er lansert!
tags: ["csharp", "ssg-prosjekt", "github", "scriban"]
---

**Min nye nettside er live på Github! Pen, innholdsrik og interessant er den ikke, men det fungerer. Og jeg har programmert det selv.**

Å se resultater av noe jeg har lært og laget selv, for "hånd", er fortsatt en av de beste følelsene å kjenne på (en følelse jeg forresten er redd mange går glipp av med for mye KI-bruk.

Hver linje i denne simple SSG-applikajsonen er kodet av meg, og jeg forstår hva alle delene gjør. Jeg har selv bestemt struktur, oppsett og logikk. Det er selvsagt ikke dermed automatisk et godt håndverk, lang der i fra, men det ligger i alle fall læring, strevsomhet og arbeid bak. 

Og det skal selvsagt ikke stoppe her. Planen er å gjøre dette så bra som mulig. 

Men å se at det faktisk fungerer, og at jeg kan lage innhold som vises på nettsiden, er en stor seier for meg. Jeg har nå et fundament å bygge videre på.

## Forberedelser

Før jeg kunne lansere, måtte jeg gjøre noen forberedelser. 

Det første var å oppdatere SiteConfig.cs til å inkludere `siteUrl` og `baseUrl`. Dette er nødvendig for å kunne generere riktige lenker til innlegg og sider. 

```csharp
public string SiteUrl { get; set; } = string.Empty;
public string BasePath { get; set; } = string.Empty;
```

Lenker i koden måtte også oppdateres til å bruke disse verdiene, slik at alle lenker blir relative til `siteUrl` og `baseUrl`. Dette er viktig for å sikre at lenker fungerer korrekt, uavhengig av hvor nettsiden hostes.

I forbindelse med dette stilte jeg meg spørsmålet om `siteconfig.json` bør automatisk oppdateres ved endringer i `SiteConfig.js`. Så vidt jeg forstå er det en dårlig ide.

>  Nye verdier bør legges til manuelt i JSON. Man bør aldri oppdatere en konfig-fil, fordi den er brukeren sin. Den kan være formatert, inndelt og sortert på spesiell måte. Legg derfor heller til info om nye valg i dokumentasjon og oppdateringsmeldinger.

## Github

Jeg etablerte Github pages i mitt eksisterende repo via actions, som bygger siden, og hoster `/output`-mappen.

Jeg måtte oppdatere instruksjonene til å bruke .NET 10, samt min kommando for å kjøre programmet og output-bane. Her er hele oppsettet:

```YAML
# Bygger nettsiden med generatoren og publiserer den på GitHub Pages
name: Bygg og publiser nettsiden

on:
  push:
    branches: ["master"]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      # NY: installer .NET
      - name: Setup .NET
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: '10.0.x'

      # NY: kjør generatoren fra rotmappen
      - name: Bygg nettsiden
        run: dotnet run --project src/CsharpBlogg.Generator -c Release

      - name: Setup Pages
        uses: actions/configure-pages@v5

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          # ENDRET: bare den genererte nettsiden, ikke hele repoet
          path: 'output'

      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v5
 ``` 

 Nå oppdateres siden automatisk hver gang jeg pusher til master.

 ## Oppdatering av maler

Scriban støttet ikke formatering av `DateTimeOffset` som jeg hadde definert i `ContentItem`-modellen. Jeg måtte derfor endre til `DateTime`.

La ellers inn `basepath` i `header.html`:

```html
<header>
	<h1><a href="{{ basepath }}">My homepage</a></h1>
</header>
```

La også inn sortering og reversering av blogg-listen i malen:

```html
{{~ for post in posts | array.sort "published" | array.reverse  ~}}
```
