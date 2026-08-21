using Google.Protobuf;
using Grpc.Core;
using Ocr;

namespace AggregatorService.Services;

public sealed class OcrGrpcService : IOcrService
{
    private static readonly TimeSpan OcrDeadline = TimeSpan.FromMinutes(15);

    private readonly Ocr.OcrService.OcrServiceClient _client;

    public OcrGrpcService(Ocr.OcrService.OcrServiceClient client)
    {
        _client = client;
    }

    public static string MapOcrLanguage(string? language)
    {
        var lang = (language ?? "en").Trim().ToLowerInvariant();
        if (lang.Length == 0)
            return "en";

        var primary = lang.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "en";
        primary = primary.Split('-', 2)[0];

        return primary switch
        {
            "en" or "ru" or "ko" => primary,
            _ => "en"
        };
    }

    public async Task<OcrRecognizeResult> RecognizePdfAsync(
        byte[] pdfBytes,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var request = new RecognizeDocumentRequest
        {
            Content = ByteString.CopyFrom(pdfBytes),
            FileName = "document.pdf",
            MimeType = "application/pdf",
            Language = MapOcrLanguage(language),
        };

        var callOptions = new CallOptions(deadline: DateTime.UtcNow.Add(OcrDeadline), cancellationToken: cancellationToken);
        var response = await _client.RecognizeDocumentAsync(request, callOptions).ConfigureAwait(false);

        var pages = response.Pages
            .Select(p => new OcrPageResult(p.PageNumber, p.Text ?? string.Empty))
            .ToList();

        var text = string.IsNullOrWhiteSpace(response.Text) ? null : response.Text;
        var warning = string.IsNullOrWhiteSpace(response.Warning) ? null : response.Warning;

        return new OcrRecognizeResult(text, pages, response.PageCount, warning);
    }
}
