using System.Xml.Linq;

namespace PublicDomainAudioBooks.Web;

/// <summary>
/// The podcast feed (/feeds/podcast.xml): every chapter of every book is an episode, in the order the books were
/// published and the chapters stand. Podcast apps read this address; nothing is uploaded to them. With no books yet it
/// is a valid feed with no episodes.
/// </summary>
public static class PodcastFeed
{
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
    private static readonly XNamespace Content = "http://purl.org/rss/1.0/modules/content/";

    public static XDocument Build(Library library, string siteUrl, string siteName)
    {
        siteUrl = siteUrl.TrimEnd('/');
        var channel = new XElement("channel",
            new XElement("title", siteName),
            new XElement("link", siteUrl),
            new XElement("language", "en"),
            new XElement("description", "Books in the public domain, read aloud in full, chapter by chapter."),
            new XElement(Itunes + "author", siteName),
            new XElement(Itunes + "explicit", "false"),
            new XElement(Itunes + "type", "serial"),
            new XElement(Itunes + "category", new XAttribute("text", "Arts"), new XElement(Itunes + "category", new XAttribute("text", "Books"))),
            new XElement(Itunes + "image", new XAttribute("href", $"{siteUrl}/cover.png")));

        foreach (var book in library.Books.OrderBy(b => b.PublishedOn).ThenBy(b => b.Title))
        {
            var day = book.PublishedOn!.Value.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
            foreach (var chapter in book.Chapters.OrderBy(c => c.Number))
            {
                var address = library.Media(chapter.Audio);
                if (address is null) continue;
                if (address.StartsWith('/')) address = siteUrl + address;
                var title = book.Chapters.Count == 1 ? book.Title : $"{book.Title}: {chapter.Title}";
                channel.Add(new XElement("item",
                    new XElement("title", title),
                    new XElement("link", $"{siteUrl}/books/{book.Slug}"),
                    new XElement("guid", new XAttribute("isPermaLink", "false"), $"{book.Slug}/{chapter.Number}"),
                    // A minute apart, so the chapters of a book keep their order in every app.
                    new XElement("pubDate", day.AddMinutes(chapter.Number).ToString("r")),
                    new XElement("description", $"{book.Title} by {book.Author}, {chapter.Title}. A public-domain text read aloud{(book.Narrator is null ? "" : $" by {book.Narrator}")}."),
                    new XElement(Itunes + "author", book.Author),
                    new XElement(Itunes + "duration", chapter.Seconds),
                    new XElement(Itunes + "episode", chapter.Number),
                    new XElement(Itunes + "episodeType", "full"),
                    new XElement("enclosure", new XAttribute("url", address), new XAttribute("type", "audio/mpeg"), new XAttribute("length", chapter.Bytes))));
            }
        }
        return new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement("rss", new XAttribute("version", "2.0"), new XAttribute(XNamespace.Xmlns + "itunes", Itunes), new XAttribute(XNamespace.Xmlns + "content", Content), channel));
    }
}
