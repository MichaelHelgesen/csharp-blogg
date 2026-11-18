---
title: s
---

Greit å gjøre det på denne måten når det kommer til å lage en liste over tags?, Eller kan AllTags like godt ligge direkte på felter, for å unngå å måtte kalle en metode som dette?

var siteContent = new SiteContent
{
    SiteConfig = siteConfig,
    Posts = posts,
    Pages = pages
};

siteContent.SetTags();

Du har allerede møtt dette spørsmålet to ganger, i to ulike forkledninger. Se om du kjenner det igjen:

«Når er objektet ferdig?» Mellom opprettelsen og SetTags() finnes det et SiteContent med innlegg, men uten tags. Ingenting hindrer deg i å glemme kallet, eller i å bruke objektet før det skjer. Koden ser riktig ut, men malen viser en tom tag-liste, og ingen feilmelding forteller deg hvorfor. Det kalles ofte temporal coupling: koden virker bare hvis ting skjer i riktig rekkefølge, men ingenting tvinger fram den rekkefølgen.

PostFolderPath. Der lagde du en property som regner ut verdien fra andre verdier. Tags er det samme: de er avledet fra innleggene. Så hvorfor skal de lagres separat?

Så du har tre alternativer:

Metodekall etterpå, som nå. Enkelt, men mulig å glemme.
En beregnet property, som PostFolderPath. Alltid i samsvar med innleggene, og umulig å glemme. Til gjengjeld regnes den ut på nytt hver gang den leses. Hvor mange ganger tror du malene vil lese den i et bygg, og hvor mange innlegg har du? Er det et reelt problem?
Regnes ut én gang når objektet lages. Krever at innleggene er på plass da. Hva i C# kjører garantert når et objekt opprettes?

Et kontrollspørsmål som skiller 2 og 3: hvis noen endrer Posts etter at objektet er laget, hvilket av alternativene gir da feil tags?

For magefølelsen hadde jeg rett angående 1. Det blir feil.

Hvis jeg har mange poster, vil den jo kjøre unødvenig mange ganger tenker jeg.
Etter min tanke den beste. Dette er en SSG, så siden må bygges når nye innlegg er lagt til, noe som garanterer korrekt liste, så lenge den kjører etter at post-listen er laget.

Enig, 3 passer godt. Og legg merke til at betingelsen du selv satt, «så lenge den kjører etter at postlisten er laget», er nettopp det en konstruktør kan garantere, i stedet for at du må huske det.

Men her ligger en felle det er verdt å teste selv. Se på hvordan du lager objektet i dag:

new SiteContent { SiteConfig = ..., Posts = ..., Pages = ... }

Krøllparentesene kalles en objektinitialiserer. Spørsmålet er: kjører konstruktøren før eller etter at verdiene i krøllparentesene settes? Svaret avgjør om konstruktøren i det hele tatt ser innleggene. Prøv det. Skriv ut antallet innlegg inne i konstruktøren og se hva du får.

Svaret peker deg mot hvordan innleggene må komme inn hvis konstruktøren skal kunne bruke dem.

Og så det siste hullet: hvis Posts fortsatt har en offentlig set, kan noen bytte ut listen etter at tags er regnet ut. Da er du tilbake til feil tags. Husker du init og private set fra tidligere? Her har du et konkret grunnlag for å bruke dem.

En liten justering til punkt 2: antall utregninger avhenger egentlig av hvor mange ganger malene leser tags, ikke direkte av antall innlegg. Men konklusjonen din står: hvorfor regne ut det samme mange ganger når svaret aldri endrer seg i løpet av et bygg?