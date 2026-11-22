---
title: Bilder på nettsiden!
published: 2026-11-22
tags: [csharp, ssg-prosjekt, scriban]
---

**Takket være Scribans malsystem, kan jeg nå enkelt legge til bilder! En essensiell ingrediens for min nettside.**

I mappen `/templates` opprettet jeg en undermappe som heter `/shortcodes`. Den skal jeg fylle med små maler som kan henvises til direkte i markdown-filene.

Den første jeg laget var `figure.html`, som skal brukes for å legge til bilder med bildetekst:

```html
 {{- src = $0; caption = $1; alt = $2 ?? $1 -}}
 {{- if src -}}
	<figure>
		<img src="resources/img/{{ src }}" alt="{{ alt | html.escape }}" />
		{{- if caption }}
		<figcaption>{{ caption }}</figcaption>
		{{- end }}
	</figure>
 {{- end -}}
```

I markdown-filen skriver jeg malen direkte inn i teksten:

```md
Se dette flotte bildet:

 {{ figure "image1.jpg" "En bildetekst" }}

Er det ikke fint?
```

Og her følger et faktisk eksempel:

{{ figure "triumph.jpg" "Et bilde av min nye skrivemaskin." }}
