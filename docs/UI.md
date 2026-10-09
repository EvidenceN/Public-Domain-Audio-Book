# The interface: NeoUI Extra

The pages are built from **NeoUI Extra** (github.com/EvidenceN/NEO-UI-EXTRA: the owner's fork of NeoUI plus companion
components), as its consumer guide asks: components first, then its utilities, and no stylesheet of our own. Every page
is static server rendering: no live connection to the server, so the site runs on any host and behind any proxy.

## How the library gets here

It is not published to a package feed yet. Its four packages, built from that repository's `main` with
`scripts/pack.sh`, are kept in `packages/` and found through `nuget.config`:

```
NeoUI.Icons.Lucide, NeoUI.Blazor.Primitives, NeoUI.Blazor, NeoUI.Extra.Blazor   4.1.38.1   (main at c9130f11, 2026-10-09)
```

To take a newer build: in NEO-UI-EXTRA run `FORK_VERSION=4.1.38.<n> scripts/pack.sh`, replace the four files in
`packages/`, change the version in `src/PublicDomainAudioBooks.Web/PublicDomainAudioBooks.Web.csproj`, run the gate.
When the library is published to a feed, `packages/` and the first source in `nuget.config` go away.

## Changes asked of the library

Anything this project needs changed in NEO-UI-EXTRA goes on its branch **`audio-book-recorded`**, never on its `main`
(owner, 2026-10-09); the agent that looks after the library reviews and merges it.

| What | Why | Until it is in |
|---|---|---|
| `PageHeader`: a slot that can be written as an element | Its `Meta` slot cannot be written `<Meta>…</Meta>` in an app: Razor reads it as the HTML `<meta>` element and refuses it (RZ1042), though the catalog's example shows exactly that | The slot is passed as an attribute: `Meta="@Fragment"` |
| An audio player component | A chapter is the browser's own `<audio>`; the one rule in `wwwroot/app.css` makes it full width | That rule |
| `LinkCard`: a portrait media shape | A book cover is taller than wide; the card's media area is landscape, so a portrait cover is cropped | The grid uses the default shape |
