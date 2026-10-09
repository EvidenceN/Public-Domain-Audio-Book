# The content folder: what a book looks like to the site

The site shows what is in its content folder (`CONTENT_DIR`, `./content` by default) and nothing else. Whatever makes
the recordings (a pipeline, or a person) hands the site a book by writing one file and uploading the audio. There is no
database and no admin: the folder is the record, and it can live in git.

```
content/
  books/
    pride-and-prejudice/
      book.md          the book: facts between two lines of ---, then its description in Markdown
```

The recordings and covers are not in the folder. They are in object storage, and the file names them
(`media:books/pride-and-prejudice/01.mp3` is fetched from `MEDIA_BASE_URL` + `/books/pride-and-prejudice/01.mp3`); a full
`https://` address works too. On a machine without object storage, set `MEDIA_DIR` to a folder and the site serves it
at `/media`.

## book.md

```markdown
---
title: Pride and Prejudice
author: Jane Austen
year: 1813                      # first published
language: en
genres: [Novel, Romance]
source_url: https://…           # where the text came from (the edition's own page)
edition: The text of the 1813 first edition.
public_domain:
  us: true                      # someone checked: public domain in the United States
  basis: First published in 1813
  checked: 2026-10-09
  excluded: The 1894 illustrations and the editor's preface were left out.   # optional
narrator: A synthetic voice
cover: media:books/pride-and-prejudice/cover.jpg   # optional; a cover is drawn from the title without one
youtube_id: dQw4w9WgXcQ         # optional: the whole book as one video
published: 2026-11-01           # the day it appears on the site; later than today keeps it hidden
chapters:
  - title: Chapter 1
    audio: media:books/pride-and-prejudice/01.mp3
    seconds: 312
    bytes: 2496000              # the file's size, for podcast apps
    youtube_id: …               # optional
  - title: Chapter 2
    audio: media:books/pride-and-prejudice/02.mp3
    seconds: 420
    bytes: 3360000
---
A paragraph or two about the book, in Markdown. No HTML.
```

Chapters are numbered in the order they are listed unless `number:` says otherwise.

## What keeps a book off the site

A book is shown only when all of this is true. A file that fails is not an error page: the book is simply not listed,
and `GET /held.json` says why, book by book.

1. **It is on record as public domain in the United States**: `public_domain.us: true`, with `basis` (why) and
   `checked` (when). This is the rule that matters most. No record, no book.
2. It says where its text came from (`source_url`).
3. It has a title, an author, and at least one chapter, and every chapter has a recording.
4. Its `published` date has come.

## What the site makes from it

- A page for each book (`/books/<folder name>`), the list (`/books`, with search and kinds of book), a page for each
  author, and the home page's latest books.
- The podcast feed, `/feeds/podcast.xml`: every chapter is an episode, books in the order they were published, chapters
  in their own order.
- `/sitemap.xml`, and `/healthz` (how many books are shown and how many are held).

The site reads the folder again when a file in it changes; nothing needs restarting.
