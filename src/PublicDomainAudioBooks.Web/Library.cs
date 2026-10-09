using System.Text.RegularExpressions;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PublicDomainAudioBooks.Web;

/// <summary>The record that a book is in the public domain: without it a book is never shown.</summary>
public sealed class PublicDomainRecord
{
    /// <summary>True only when someone has checked that this text is in the public domain in the United States.</summary>
    public bool Us { get; set; }
    /// <summary>Why: "first published in 1813", "author died in 1910 and the work was published before 1931".</summary>
    public string? Basis { get; set; }
    /// <summary>The date it was checked (yyyy-MM-dd).</summary>
    public string? Checked { get; set; }
    /// <summary>What is NOT covered: a later translation, introduction, notes or illustrations that were left out.</summary>
    public string? Excluded { get; set; }
}

public sealed class Chapter
{
    public int Number { get; set; }
    public string Title { get; set; } = "";
    /// <summary>The recording: "media:books/pride-and-prejudice/01.mp3" (under MEDIA_BASE_URL) or a full address.</summary>
    public string? Audio { get; set; }
    public int Seconds { get; set; }
    public long Bytes { get; set; }
    public string? YoutubeId { get; set; }
}

/// <summary>A book as its file describes it (content/books/&lt;slug&gt;/book.md: this as YAML between "---" lines, then the description).</summary>
public sealed class Book
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    /// <summary>The year the text was first published.</summary>
    public int? Year { get; set; }
    public string Language { get; set; } = "en";
    public List<string> Genres { get; set; } = [];
    /// <summary>Where the text came from (the edition's page), shown as "Source text".</summary>
    public string? SourceUrl { get; set; }
    /// <summary>Which edition or translation was read, in a sentence.</summary>
    public string? Edition { get; set; }
    public PublicDomainRecord PublicDomain { get; set; } = new();
    /// <summary>The voice that reads it, said plainly ("a synthetic voice").</summary>
    public string? Narrator { get; set; }
    public string? Cover { get; set; }
    /// <summary>The whole book as one video, when there is one.</summary>
    public string? YoutubeId { get; set; }
    /// <summary>When it goes on the site (yyyy-MM-dd); a later date keeps it hidden until then.</summary>
    public string? Published { get; set; }
    public List<Chapter> Chapters { get; set; } = [];

    [YamlIgnore] public string DescriptionHtml { get; set; } = "";
    [YamlIgnore] public string Excerpt { get; set; } = "";

    [YamlIgnore] public int Seconds => Chapters.Sum(c => c.Seconds);
    [YamlIgnore] public DateOnly? PublishedOn => DateOnly.TryParse(Published, out var d) ? d : null;
    [YamlIgnore] public string AuthorSlug => Library.Slug(Author);
}

/// <summary>
/// The books, read from the content folder (CONTENT_DIR). The folder is the whole database: a pipeline (or a person)
/// writes book files into it and the site shows them. A book appears only when it is ready to be heard and is on
/// record as public domain; see <see cref="Problems"/> for why one is not shown.
/// </summary>
public sealed partial class Library(string contentDir, string? mediaBaseUrl)
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build();
    private static readonly MarkdownPipeline Markdown = new MarkdownPipelineBuilder().UseAdvancedExtensions().DisableHtml().Build();

    private readonly object _gate = new();
    private string _stamp = "";
    private List<Book> _books = [];
    private List<(string Slug, string Reason)> _held = [];

    /// <summary>Every book that may be shown, newest first.</summary>
    public IReadOnlyList<Book> Books { get { Load(); return _books; } }

    /// <summary>Book files that are there but not shown, each with the reason.</summary>
    public IReadOnlyList<(string Slug, string Reason)> Held { get { Load(); return _held; } }

    public Book? Find(string slug) => Books.FirstOrDefault(b => b.Slug == slug);

    public IEnumerable<IGrouping<string, Book>> Authors => Books.GroupBy(b => b.Author).OrderBy(g => g.Key);

    /// <summary>Where a recording or a cover is fetched from: a full address as it is, "media:key" under MEDIA_BASE_URL.</summary>
    public string? Media(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        if (reference.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || reference.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return reference;
        var key = reference.StartsWith("media:", StringComparison.Ordinal) ? reference[6..] : reference;
        return $"{(mediaBaseUrl ?? "/media").TrimEnd('/')}/{string.Join('/', key.Split('/').Select(Uri.EscapeDataString))}";
    }

    /// <summary>
    /// Why a book must not be shown, or nothing when it may. The first rule is the one that matters most: no record that
    /// the text is public domain in the United States, no book.
    /// </summary>
    public static List<string> Problems(Book book, DateOnly today)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(book.Title)) problems.Add("it has no title");
        if (string.IsNullOrWhiteSpace(book.Author)) problems.Add("it has no author");
        if (!book.PublicDomain.Us) problems.Add("it is not on record as public domain in the United States (public_domain.us)");
        if (string.IsNullOrWhiteSpace(book.PublicDomain.Basis)) problems.Add("the record does not say why it is public domain (public_domain.basis)");
        if (string.IsNullOrWhiteSpace(book.PublicDomain.Checked)) problems.Add("the record does not say when that was checked (public_domain.checked)");
        if (string.IsNullOrWhiteSpace(book.SourceUrl)) problems.Add("it does not say where its text came from (source_url)");
        if (book.Chapters.Count == 0) problems.Add("it has no chapters");
        else if (book.Chapters.Any(c => string.IsNullOrWhiteSpace(c.Audio))) problems.Add("a chapter has no recording");
        if (book.PublishedOn is null) problems.Add("it has no publication date (published)");
        else if (book.PublishedOn > today) problems.Add($"it is to be published on {book.Published}");
        return problems;
    }

    /// <summary>A book from the text of its file.</summary>
    public static Book Parse(string slug, string text)
    {
        var match = Frontmatter().Match(text);
        if (!match.Success) throw new FormatException("the file does not begin with a block between two lines of ---");
        var book = Yaml.Deserialize<Book>(match.Groups[1].Value) ?? new Book();
        book.Slug = slug;
        var body = text[match.Length..].Trim();
        book.DescriptionHtml = Markdig.Markdown.ToHtml(body, Markdown);
        var plain = Regex.Replace(Regex.Replace(book.DescriptionHtml, "<[^>]+>", " "), @"\s+", " ").Trim();
        book.Excerpt = plain.Length > 220 ? plain[..220].TrimEnd() + "…" : plain;
        for (var i = 0; i < book.Chapters.Count; i++)
            if (book.Chapters[i].Number == 0) book.Chapters[i].Number = i + 1;
        return book;
    }

    public static string Slug(string text) => Dashes().Replace(text.ToLowerInvariant(), "-").Trim('-');

    public static string Length(int seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours} hr {t.Minutes} min" : $"{Math.Max(1, t.Minutes)} min";
    }

    private void Load()
    {
        var folder = Path.Combine(contentDir, "books");
        var files = Directory.Exists(folder) ? Directory.GetFiles(folder, "book.md", SearchOption.AllDirectories).Order().ToList() : [];
        var stamp = $"{DateOnly.FromDateTime(DateTime.UtcNow)}|" + string.Join("|", files.Select(f => $"{f}:{File.GetLastWriteTimeUtc(f).Ticks}"));
        lock (_gate)
        {
            if (stamp == _stamp) return;
            var books = new List<Book>();
            var held = new List<(string, string)>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            foreach (var file in files)
            {
                var slug = Path.GetFileName(Path.GetDirectoryName(file))!;
                try
                {
                    var book = Parse(slug, File.ReadAllText(file));
                    var problems = Problems(book, today);
                    if (problems.Count == 0) books.Add(book); else held.Add((slug, string.Join("; ", problems)));
                }
                catch (Exception ex) when (ex is FormatException or YamlDotNet.Core.YamlException or IOException)
                {
                    held.Add((slug, "its file cannot be read: " + ex.Message));
                }
            }
            (_books, _held, _stamp) = ([.. books.OrderByDescending(b => b.PublishedOn).ThenBy(b => b.Title)], held, stamp);
        }
    }

    [GeneratedRegex(@"\A---\r?\n(.*?)\r?\n---\r?\n?", RegexOptions.Singleline)]
    private static partial Regex Frontmatter();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex Dashes();
}
