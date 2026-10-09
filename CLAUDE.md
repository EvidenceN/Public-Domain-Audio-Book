# CLAUDE.md — Public Domain Audio Books

Notes for Claude working in this repo. Read [README.md](README.md) first.

## What this is
The site of a library of public-domain books read aloud (owner, 2026-10-09: "start another platform/youtube
channel/podcast called Public Domain Audio Books … Take public domain books and create audio recording of them …
sourced from gutenberg.org"). It is its own product with its own repository, separate from Perfect Summary, and reuses
that project's parts as separate applications: the pipeline's narration and checking, the Complete Dictionary, and
NeoUI Extra for the interface.

## Where it stands
- **Built:** the site (this repo), its content format, its podcast feed, its tests, its Docker setup.
- **Not built, on purpose** (owner: "just build the site and infrastructure. But, don't run any of the pipelines …
  don't fill in any data and don't process any data"): choosing books, fetching texts, narration, video, uploads. Where
  that work will live (its own pipeline, or a page on Perfect Summary's pipeline admin) is not decided.
- **Do not add books or sample data to `content/`.** A sample for looking at a page lives outside the repo.

## Rules
- **A book without a public-domain record is never shown** (`Library.Problems`). Never weaken it. The checklist a
  person or a pipeline follows before recording is [docs/LEGAL.md](docs/LEGAL.md).
- **The interface is NeoUI Extra** ([docs/UI.md](docs/UI.md)): build pages from its components, then its utilities;
  do not write a stylesheet. If something is missing, record the gap in docs/UI.md and propose it on NEO-UI-EXTRA's
  branch **`audio-book-recorded`**. **Never merge anything to that repository's `main`**: its own agent reviews and merges.
- **Static server rendering only.** No interactive render mode unless a page truly needs one: the site must run on any
  host without sticky sessions.
- **The site makes nothing.** It reads `CONTENT_DIR` and links to media in storage. No database.
- The site says plainly that a synthetic voice reads the books, and that the public-domain rule is the United States'.
  Keep both.
- Do not use a source's name or logo to promote the recordings (see docs/LEGAL.md, point 4).
- Branch → PR → `tools/ci/local.sh` must end with `OK` → merge. Tests run on this machine, not on GitHub Actions.
- Port on the owner's machine: **8220**. Large or local-only files: `C:\Users\Evidence The Great\0_PerfectSummaryM5`.

## Layout
- `src/PublicDomainAudioBooks.Web`: `Library.cs` (book files, the rules for showing one), `PodcastFeed.cs`,
  `Program.cs` (settings, feed, sitemap, health), `Components/` (pages and layout).
- `tests/`: the book format, the rules, the feed. No network.
- `content/`: empty. `packages/`: NeoUI Extra's packages (see docs/UI.md).
