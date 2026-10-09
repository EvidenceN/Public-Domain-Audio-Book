using System.Xml.Linq;
using PublicDomainAudioBooks.Web;

namespace PublicDomainAudioBooks.Tests;

public sealed class LibraryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "pdab-tests-" + Guid.NewGuid().ToString("N"));

    private const string Good = """
        ---
        title: Pride and Prejudice
        author: Jane Austen
        year: 1813
        genres: [Novel, Romance]
        source_url: https://example.org/texts/1342
        edition: The text of the 1813 first edition.
        public_domain:
          us: true
          basis: First published in 1813
          checked: 2026-10-09
        narrator: A synthetic voice
        published: 2026-01-02
        chapters:
          - title: Chapter 1
            audio: media:books/pride-and-prejudice/01.mp3
            seconds: 300
            bytes: 2400000
          - title: Chapter 2
            audio: media:books/pride-and-prejudice/02.mp3
            seconds: 420
            bytes: 3300000
        ---
        It is a truth universally acknowledged that this is a **description**.
        """;

    private void Write(string slug, string text)
    {
        Directory.CreateDirectory(Path.Combine(_folder, "books", slug));
        File.WriteAllText(Path.Combine(_folder, "books", slug, "book.md"), text.Replace("\r\n", "\n"));
    }

    [Fact]
    public void A_book_file_is_read_whole()
    {
        var book = Library.Parse("pride-and-prejudice", Good.Replace("\r\n", "\n"));
        Assert.Equal("Pride and Prejudice", book.Title);
        Assert.Equal("Jane Austen", book.Author);
        Assert.Equal(1813, book.Year);
        Assert.True(book.PublicDomain.Us);
        Assert.Equal(["Novel", "Romance"], book.Genres);
        Assert.Equal([1, 2], book.Chapters.Select(c => c.Number));
        Assert.Equal(720, book.Seconds);
        Assert.Contains("<strong>description</strong>", book.DescriptionHtml);
        Assert.Equal("jane-austen", book.AuthorSlug);
        Assert.Empty(Library.Problems(book, new DateOnly(2026, 10, 9)));
    }

    [Theory]
    [InlineData("  us: true", "  us: false", "not on record as public domain")]
    [InlineData("  basis: First published in 1813\n", "", "does not say why")]
    [InlineData("  checked: 2026-10-09\n", "", "when that was checked")]
    [InlineData("source_url: https://example.org/texts/1342\n", "", "where its text came from")]
    [InlineData("published: 2026-01-02", "published: 2030-01-01", "to be published on")]
    [InlineData("    audio: media:books/pride-and-prejudice/02.mp3\n", "", "has no recording")]
    public void A_book_that_is_not_ready_or_not_on_record_as_public_domain_is_never_shown(string from, string to, string reason)
    {
        var text = Good.Replace("\r\n", "\n");
        Assert.Contains(from, text);
        var book = Library.Parse("x", text.Replace(from, to));
        Assert.Contains(Library.Problems(book, new DateOnly(2026, 10, 9)), p => p.Contains(reason));
    }

    [Fact]
    public void The_library_shows_good_books_and_says_why_it_holds_the_others()
    {
        Write("pride-and-prejudice", Good);
        Write("not-checked", Good.Replace("  us: true", "  us: false").Replace("Pride and Prejudice", "Not Checked"));
        Write("broken", "no frontmatter here");
        var library = new Library(_folder, "https://media.example.org/");
        Assert.Equal(["pride-and-prejudice"], library.Books.Select(b => b.Slug));
        Assert.Equal(2, library.Held.Count);
        Assert.Contains(library.Held, h => h.Slug == "not-checked" && h.Reason.Contains("public domain"));
        Assert.Contains(library.Held, h => h.Slug == "broken" && h.Reason.Contains("cannot be read"));
        Assert.Equal("https://media.example.org/books/pride-and-prejudice/01.mp3", library.Media(library.Books[0].Chapters[0].Audio));
        Assert.Equal("https://elsewhere.example/a.mp3", library.Media("https://elsewhere.example/a.mp3"));
    }

    [Fact]
    public void An_empty_library_is_empty_and_its_feed_is_still_a_feed()
    {
        var library = new Library(_folder, null);
        Assert.Empty(library.Books);
        var feed = PodcastFeed.Build(library, "https://example.org", "Public Domain Audio Books");
        Assert.Equal("rss", feed.Root!.Name.LocalName);
        Assert.Empty(feed.Descendants("item"));
    }

    [Fact]
    public void Every_chapter_is_an_episode_of_the_feed_in_order()
    {
        Write("pride-and-prejudice", Good);
        var feed = PodcastFeed.Build(new Library(_folder, null), "https://example.org/", "Public Domain Audio Books");
        var items = feed.Descendants("item").ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("Pride and Prejudice: Chapter 1", items[0].Element("title")!.Value);
        Assert.Equal("https://example.org/media/books/pride-and-prejudice/01.mp3", items[0].Element("enclosure")!.Attribute("url")!.Value);
        Assert.Equal("2400000", items[0].Element("enclosure")!.Attribute("length")!.Value);
        Assert.True(DateTime.Parse(items[0].Element("pubDate")!.Value) < DateTime.Parse(items[1].Element("pubDate")!.Value));
        XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        Assert.Equal("Jane Austen", items[0].Element(itunes + "author")!.Value);
    }

    public void Dispose() { try { Directory.Delete(_folder, recursive: true); } catch (IOException) { } }
}
