using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.FileProviders;
using NeoUI.Extra;
using PublicDomainAudioBooks.Web;
using PublicDomainAudioBooks.Web.Components;

// The site of Public Domain Audio Books. It only shows what is in its content folder: it makes nothing itself.
//   CONTENT_DIR      the folder of book files (default: ./content). See docs/CONTENT.md.
//   MEDIA_BASE_URL   where recordings and covers are fetched from (object storage). Unset: MEDIA_DIR is served at /media.
//   MEDIA_DIR        a folder of media files to serve at /media, for a machine without object storage.
//   SITE_URL         the site's public address, used in the podcast feed and the sitemap.
//   SITE_NAME        its name (default: Public Domain Audio Books).
var builder = WebApplication.CreateBuilder(args);
var settings = new SiteSettings(
    Environment.GetEnvironmentVariable("SITE_NAME") is { Length: > 0 } name ? name : "Public Domain Audio Books",
    (Environment.GetEnvironmentVariable("SITE_URL") is { Length: > 0 } url ? url : "http://localhost:8220").TrimEnd('/'));
var contentDir = Environment.GetEnvironmentVariable("CONTENT_DIR") is { Length: > 0 } c ? c : Path.Combine(builder.Environment.ContentRootPath, "content");
var mediaBase = Environment.GetEnvironmentVariable("MEDIA_BASE_URL") is { Length: > 0 } m ? m : null;
var mediaDir = Environment.GetEnvironmentVariable("MEDIA_DIR") is { Length: > 0 } d ? d : null;

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(new Library(contentDir, mediaBase));
builder.Services.AddRazorComponents();
// One call registers NeoUI's primitives and components and NeoUI Extra.
builder.Services.AddNeoUIExtra();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
if (mediaBase is null && mediaDir is not null && Directory.Exists(mediaDir))
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.GetFullPath(mediaDir)), RequestPath = "/media" });

app.MapGet("/healthz", (Library library) => Results.Json(new { status = "ok", books = library.Books.Count, held = library.Held.Count }));

// Why a book file is not shown. Not linked from the site: for whoever feeds it.
app.MapGet("/held.json", (Library library) => Results.Json(library.Held.Select(h => new { book = h.Slug, reason = h.Reason })));

app.MapGet("/feeds/podcast.xml", (Library library) => Xml(PodcastFeed.Build(library, settings.Url, settings.Name), "application/rss+xml"));

app.MapGet("/sitemap.xml", (Library library) =>
{
    XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
    var pages = new[] { "", "/books", "/authors", "/about" }
        .Concat(library.Books.Select(b => $"/books/{b.Slug}"))
        .Concat(library.Authors.Select(a => $"/authors/{Library.Slug(a.Key)}"));
    return Xml(new XDocument(new XElement(ns + "urlset", pages.Select(p => new XElement(ns + "url", new XElement(ns + "loc", settings.Url + p))))), "application/xml");
});

app.MapGet("/robots.txt", () => Results.Text($"User-agent: *\nAllow: /\nSitemap: {settings.Url}/sitemap.xml\n"));

app.MapRazorComponents<App>();
app.Run();

static IResult Xml(XDocument document, string type)
{
    using var writer = new Utf8Writer();
    document.Save(writer);
    return Results.Text(writer.ToString(), type, Encoding.UTF8);
}

/// <summary>The site's name and public address.</summary>
public sealed record SiteSettings(string Name, string Url);

/// <summary>A text writer that says it writes UTF-8, so the XML declares the encoding it is served in.</summary>
public sealed class Utf8Writer : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}

public partial class Program;
