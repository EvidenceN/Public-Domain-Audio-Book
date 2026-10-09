# Public Domain Audio Books

Books in the public domain, read aloud in full, chapter by chapter: a site, a podcast feed, and (later) a YouTube
channel. This repository is the **site**. It shows what is in its content folder and makes nothing itself; whatever
records the books (its own pipeline, or a page of Perfect Summary's pipeline: not decided) hands it books as files.

**Status (2026-10-09): the site and its infrastructure are built; there are no books in it and nothing has been
recorded.** With an empty content folder it runs and says so.

```bash
docker compose up --build        # http://127.0.0.1:8220
```

Without Docker: `dotnet run --project src/PublicDomainAudioBooks.Web`.

| | |
|---|---|
| What a book looks like to the site, and what keeps one off it | [docs/CONTENT.md](docs/CONTENT.md) |
| The public-domain checklist before a book is recorded | [docs/LEGAL.md](docs/LEGAL.md) |
| The interface (NeoUI Extra), and what was asked of that library | [docs/UI.md](docs/UI.md) |
| Notes for Claude working here | [CLAUDE.md](CLAUDE.md) |

## What it does

- `/` the home page, `/books` (search, kinds of book), `/books/<book>` (description, a player and a download for every
  chapter, the whole book on YouTube when there is one, why the book is public domain and where its text came from),
  `/authors`, `/about`.
- `/feeds/podcast.xml`: every chapter of every book as a podcast episode.
- `/sitemap.xml`, `/robots.txt`, `/healthz`, `/held.json` (book files that are not shown, and why).
- **A book without a record that it is public domain in the United States is never shown.**

## Settings

| Variable | Meaning | Default |
|---|---|---|
| `CONTENT_DIR` | The folder of book files | `./content` |
| `MEDIA_BASE_URL` | Where recordings and covers are fetched from (object storage) | unset: `/media` |
| `MEDIA_DIR` | A folder served at `/media` when there is no object storage | unset |
| `SITE_URL` | The public address, for the feed and the sitemap | `http://localhost:8220` |
| `SITE_NAME` | The name shown | `Public Domain Audio Books` |

## Working on it

`tools/ci/local.sh` builds, runs the tests and (unless `quick`) starts the site in Docker with an empty content folder
and checks every page answers. Tests run on this machine, not on GitHub Actions.
