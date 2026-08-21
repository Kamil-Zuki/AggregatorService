using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using VersOne.Epub;

namespace AggregatorService.Services;

public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    private readonly IOcrService _ocrService;

    public DocumentTextExtractor(IOcrService ocrService)
    {
        _ocrService = ocrService;
    }

    private static readonly Regex HtmlStripScripts = new(@"<script[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlStripStyles = new(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlStripTags = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceCollapse = new(@"\s+", RegexOptions.Compiled);

    public async Task<ExtractDocumentTextResult> ExtractAsync(
        Stream stream,
        string fileName,
        string? contentType,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var bytes = ms.ToArray();
        if (bytes.Length == 0)
        {
            throw new NoExtractableDocumentTextException(
                ResolveFormat(fileName, contentType),
                "EMPTY_FILE",
                "The uploaded file is empty.");
        }

        ms.Position = 0;
        var format = ResolveFormat(fileName, contentType);
        return format switch
        {
            "txt" => ExtractTxt(ms, fileName),
            "pdf" => await ExtractPdfAsync(ms, bytes, language, cancellationToken).ConfigureAwait(false),
            "epub" => await ExtractEpubAsync(ms, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported document format for extraction: {format}")
        };
    }

    private static ExtractDocumentTextResult ExtractTxt(MemoryStream ms, string fileName)
    {
        ms.Position = 0;
        using var reader = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = reader.ReadToEnd();
        var normalized = NormalizeWithinPage(text);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new NoExtractableDocumentTextException(
                "txt",
                "TXT_NO_TEXT",
                "The text file contains no readable characters.");
        }

        var title = Path.GetFileNameWithoutExtension(fileName);
        var pages = new[] { new ExtractDocumentTextPage(1, normalized) };
        return new ExtractDocumentTextResult(
            normalized,
            string.IsNullOrWhiteSpace(title) ? null : title,
            "txt",
            pages,
            UsedOcr: false);
    }

    private async Task<ExtractDocumentTextResult> ExtractPdfAsync(
        MemoryStream ms,
        byte[] bytes,
        string? language,
        CancellationToken cancellationToken)
    {
        ms.Position = 0;
        List<ExtractDocumentTextPage> pages;
        try
        {
            using var pdf = PdfDocument.Open(ms);
            pages = [];
            var pageNumber = 0;
            foreach (var page in pdf.GetPages())
            {
                pageNumber++;
                var pageText = NormalizeWithinPage(page.Text ?? string.Empty);
                if (!string.IsNullOrEmpty(pageText))
                    pages.Add(new ExtractDocumentTextPage(pageNumber, pageText));
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not read PDF bytes as a PDF document.", ex);
        }

        if (pages.Count > 0)
        {
            var fullText = JoinPages(pages);
            var title = TryReadPdfTitle(ms);
            return new ExtractDocumentTextResult(fullText, title, "pdf", pages, UsedOcr: false);
        }

        var ocr = await _ocrService.RecognizePdfAsync(bytes, language, cancellationToken).ConfigureAwait(false);
        var ocrPages = ocr.Pages
            .Select(p => new ExtractDocumentTextPage(p.PageNumber, NormalizeWithinPage(p.Text)))
            .Where(p => !string.IsNullOrEmpty(p.Text))
            .ToList();

        if (ocrPages.Count == 0)
        {
            throw new NoExtractableDocumentTextException(
                "pdf",
                "PDF_NO_TEXT_LAYER",
                "This PDF has no selectable text layer and OCR produced no readable text.");
        }

        var ocrTitle = TryReadPdfTitle(ms);
        return new ExtractDocumentTextResult(
            JoinPages(ocrPages),
            ocrTitle,
            "pdf+ocr",
            ocrPages,
            UsedOcr: true,
            Warning: ocr.Warning);
    }

    private static async Task<ExtractDocumentTextResult> ExtractEpubAsync(MemoryStream ms, CancellationToken cancellationToken)
    {
        ms.Position = 0;
        EpubBook book;
        try
        {
            book = await EpubReader.ReadBookAsync(ms).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not read EPUB archive.", ex);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var pages = new List<ExtractDocumentTextPage>();
        var chapterIndex = 0;
        foreach (var chapter in book.ReadingOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();
            chapterIndex++;
            var fragment = StripHtml(chapter.Content);
            if (fragment.Length > 0)
                pages.Add(new ExtractDocumentTextPage(chapterIndex, fragment));
        }

        if (pages.Count == 0)
        {
            throw new NoExtractableDocumentTextException(
                "epub",
                "EPUB_NO_TEXT",
                "This EPUB has no readable text content in its spine.");
        }

        var title = string.IsNullOrWhiteSpace(book.Title) ? null : book.Title.Trim();
        return new ExtractDocumentTextResult(JoinPages(pages), title, "epub", pages, UsedOcr: false);
    }

    private static string? TryReadPdfTitle(MemoryStream ms)
    {
        try
        {
            ms.Position = 0;
            using var pdf = PdfDocument.Open(ms);
            var info = pdf.Information;
            var title = info?.Title;
            return string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        }
        catch
        {
            return null;
        }
    }

    public static string ResolveFormat(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        var ct = contentType ?? string.Empty;

        if (ext is ".txt" || ct.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
            return "txt";

        if (ext is ".pdf" || ct.Contains("application/pdf", StringComparison.OrdinalIgnoreCase))
            return "pdf";

        if (ext is ".epub" || ct.Contains("application/epub+zip", StringComparison.OrdinalIgnoreCase))
            return "epub";

        return "unknown";
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var s = HtmlStripScripts.Replace(html, " ");
        s = HtmlStripStyles.Replace(s, " ");
        s = HtmlStripTags.Replace(s, " ");
        s = WebUtility.HtmlDecode(s);
        return NormalizeWithinPage(s);
    }

    /// <summary>Collapse runs of whitespace inside a single page/chapter; preserve non-empty result.</summary>
    private static string NormalizeWithinPage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return WhitespaceCollapse.Replace(text.Trim(), " ");
    }

    private static string JoinPages(IReadOnlyList<ExtractDocumentTextPage> pages)
        => string.Join("\n\n", pages.Select(p => p.Text));
}
