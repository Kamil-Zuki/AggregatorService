using System.Net;

using AggregatorService.Dtos;
using AggregatorService.Helpers;
using AggregatorService.Services;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pvs.Media.Grpc;

namespace AggregatorService.Controllers;

/// <summary>
/// Контроллер для загрузки медиа (изображения для карточек и т.д.).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxDocumentSizeBytes = 50 * 1024 * 1024; // 50 MB
    private static readonly System.Text.Json.JsonSerializerOptions ExtractJsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    private readonly IMediaServiceClient _mediaServiceClient;
    private readonly IAuthorizationServiceClient _authorizationServiceClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDocumentTextExtractor _documentTextExtractor;
    private readonly ITtsAudioService _ttsAudioService;
    private readonly IBillingServiceClient _billingServiceClient;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IMediaServiceClient mediaServiceClient,
        IAuthorizationServiceClient authorizationServiceClient,
        IHttpClientFactory httpClientFactory,
        IDocumentTextExtractor documentTextExtractor,
        ITtsAudioService ttsAudioService,
        IBillingServiceClient billingServiceClient,
        ILogger<MediaController> logger)
    {
        _mediaServiceClient = mediaServiceClient;
        _authorizationServiceClient = authorizationServiceClient;
        _httpClientFactory = httpClientFactory;
        _documentTextExtractor = documentTextExtractor;
        _ttsAudioService = ttsAudioService;
        _billingServiceClient = billingServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Загружает изображение в хранилище. Используется редактором карточек (вставка из буфера, выбор файла).
    /// </summary>
    /// <param name="file">Файл изображения (multipart/form-data, поле "file")</param>
    /// <returns>URL загруженного изображения</returns>
    [HttpPost("upload-image")]
    [ProducesResponseType(typeof(UploadImageResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadImageResponseDto>> UploadImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest(new { error = $"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB" });
        }

        var contentType = file.ContentType ?? "image/png";
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Content type must be image/*" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, HttpContext.RequestAborted).ConfigureAwait(false);
            var bytes = ms.ToArray();

            var request = new Pvs.Media.Grpc.UploadImageRequest
            {
                ImageData = ByteString.CopyFrom(bytes),
                ContentType = contentType
            };

            var response = await _mediaServiceClient.UploadImageAsync(
                request,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            _logger.LogInformation("Image uploaded for user {UserId}, URL returned", userId);

            // Нельзя передавать DTO вторым аргументом в CreatedAtAction — там routeValues; иначе тело ответа пустое и клиент не получает url.
            var dto = new UploadImageResponseDto { Url = response.Url, ImageId = response.ImageId };
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid image data" });
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
            _logger.LogError(ex, "Media storage is unavailable");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Status.Detail ?? "Media storage unavailable" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while uploading image");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to upload image" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized upload attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return new ObjectResult(new { error = "Failed to upload image" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Синтез аудио через внешний TTS (OpenAI-compatible) и загрузка в MediaService.
    /// Gated by EnableAIAgents (MVP uses browser TTS when disabled).
    /// </summary>
    [HttpPost("generate-audio")]
    [AggregatorService.Filters.FeatureFlagFilter("EnableAIAgents")]
    [ProducesResponseType(typeof(GenerateAudioResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GenerateAudioResponseDto>> GenerateAudio([FromBody] GenerateAudioRequestDto? body)
    {
        if (body == null)
            return BadRequest(new { error = "Request body is required" });

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var dto = await _ttsAudioService
                .GenerateAndStoreAsync(body, userId, roles, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            _logger.LogInformation("TTS audio stored for user {UserId}", userId);
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "TTS not available");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "TTS provider error");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid audio upload" });
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
            _logger.LogError(ex, "Media storage is unavailable");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Status.Detail ?? "Media storage unavailable" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while uploading synthesized audio");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to store audio" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audio");
            return new ObjectResult(new { error = "Failed to generate audio" }) { StatusCode = 500 };
        }
    }

    private static bool TryNormalizeReaderDocumentUpload(string contentType, string fileName, out string normalized)
    {
        normalized = "";

        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "application/pdf";
            return true;
        }

        if (string.Equals(contentType, "application/epub+zip", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contentType, "application/x-epub+zip", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "application/epub+zip";
            return true;
        }

        if (string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "text/plain";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Загружает документ Reader (PDF, EPUB или TXT) в хранилище.
    /// </summary>
    [HttpPost("upload-document")]
    [ProducesResponseType(typeof(UploadDocumentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadDocumentResponseDto>> UploadDocument(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        if (file.Length > MaxDocumentSizeBytes)
        {
            return BadRequest(new { error = $"File size must not exceed {MaxDocumentSizeBytes / (1024 * 1024)} MB" });
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType.Trim();
        var fileName = file.FileName ?? string.Empty;
        if (!TryNormalizeReaderDocumentUpload(contentType, fileName, out var normalizedDocumentType))
        {
            return BadRequest(new { error = "Unsupported format. Upload PDF, EPUB (.epub), or plain text (.txt)." });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, HttpContext.RequestAborted).ConfigureAwait(false);
            var bytes = ms.ToArray();

            var request = new Pvs.Media.Grpc.UploadDocumentRequest
            {
                DocumentData = ByteString.CopyFrom(bytes),
                ContentType = normalizedDocumentType,
                FileName = fileName
            };

            var response = await _mediaServiceClient.UploadDocumentAsync(
                request,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            _logger.LogInformation("Document uploaded for user {UserId}, URL returned", userId);

            var dto = new UploadDocumentResponseDto { Url = response.Url, DocumentId = response.DocumentId };
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid document data" });
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
            _logger.LogError(ex, "Media storage is unavailable");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Status.Detail ?? "Media storage unavailable" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while uploading document");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to upload document" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document upload attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return new ObjectResult(new { error = "Failed to upload document" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Извлекает обычный текст из PDF (text layer), EPUB или TXT для Reader (paste по-прежнему через analyze).
    /// </summary>
    [HttpPost("extract-document-text")]
    [ProducesResponseType(typeof(ExtractDocumentTextResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExtractDocumentTextResponseDto>> ExtractDocumentText(
        IFormFile? file,
        [FromForm] string? language,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        if (file.Length > MaxDocumentSizeBytes)
        {
            return BadRequest(new { error = $"File size must not exceed {MaxDocumentSizeBytes / (1024 * 1024)} MB" });
        }

        var format = DocumentTextExtractor.ResolveFormat(file.FileName ?? string.Empty, file.ContentType);
        if (format == "unknown")
        {
            return BadRequest(new { error = "Unsupported format; use .pdf, .epub, or .txt" });
        }

        try
        {
            await using var uploadStream = file.OpenReadStream();
            var extracted = await _documentTextExtractor.ExtractAsync(
                    uploadStream,
                    file.FileName ?? "upload",
                    file.ContentType,
                    language,
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new ExtractDocumentTextResponseDto
            {
                Text = extracted.Text,
                Title = extracted.Title,
                SourceFormat = extracted.SourceFormat,
                UsedOcr = extracted.UsedOcr,
                Warning = extracted.Warning,
                Pages = extracted.Pages
                    .Select(p => new ExtractDocumentTextPageDto { PageNumber = p.PageNumber, Text = p.Text })
                    .ToList()
            });
        }
        catch (NoExtractableDocumentTextException ex)
        {
            _logger.LogWarning(ex, "No extractable text for {FileName} ({Format})", file.FileName, ex.SourceFormat);
            return UnprocessableEntity(new
            {
                error = "NoExtractableText",
                message = ex.Message,
                details = new
                {
                    format = ex.SourceFormat,
                    reason = ex.ReasonCode,
                    hint = ex.SourceFormat == "pdf"
                        ? "The PDF has no selectable text layer and OCR produced no readable text. Try a clearer scan or paste a transcript."
                        : "Try another file or paste the text directly."
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document extraction failed for {FileName}", file.FileName);
            return BadRequest(new { error = "InvalidDocument", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error extracting document text");
            return new ObjectResult(new { error = "Failed to extract document text" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Stores OCR/extracted text sidecar JSON for a Reader document.
    /// </summary>
    [HttpPut("documents/{documentId}/extract")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PutDocumentExtract(string documentId, [FromBody] DocumentExtractDto? extract, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documentId) || !Guid.TryParse(documentId, out _))
        {
            return BadRequest(new { error = "Valid documentId (UUID) is required" });
        }

        if (extract == null || extract.Pages.Count == 0)
        {
            return BadRequest(new { error = "Extract payload with pages is required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var json = System.Text.Json.JsonSerializer.Serialize(extract, ExtractJsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await _mediaServiceClient.PutDocumentExtractAsync(
                new PutDocumentExtractRequest
                {
                    DocumentId = documentId.Trim(),
                    ExtractJson = ByteString.CopyFrom(bytes)
                },
                userId,
                roles,
                cancellationToken).ConfigureAwait(false);

            return NoContent();
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid extract payload" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "PutDocumentExtract failed for {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Failed to store document extract" });
        }
    }

    /// <summary>
    /// Loads OCR/extracted text sidecar JSON for a Reader document.
    /// </summary>
    [HttpGet("documents/{documentId}/extract")]
    [ProducesResponseType(typeof(DocumentExtractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DocumentExtractDto>> GetDocumentExtract(string documentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documentId) || !Guid.TryParse(documentId, out _))
        {
            return BadRequest(new { error = "Valid documentId (UUID) is required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var response = await _mediaServiceClient.GetDocumentExtractAsync(
                new GetDocumentExtractRequest { DocumentId = documentId.Trim() },
                userId,
                roles,
                cancellationToken).ConfigureAwait(false);

            var json = response.ExtractJson.ToStringUtf8();
            var dto = System.Text.Json.JsonSerializer.Deserialize<DocumentExtractDto>(json, ExtractJsonOptions);
            if (dto == null || dto.Pages.Count == 0)
            {
                return NotFound(new { error = "Document extract not found" });
            }

            return Ok(dto);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(new { error = "Document extract not found" });
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid documentId" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "GetDocumentExtract failed for {DocumentId}", documentId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Failed to load document extract" });
        }
    }

    /// <summary>
    /// Проксирует PDF-документ по URL для Reader.
    /// </summary>
    [HttpGet("serve-document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ServeDocument([FromQuery] string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new { error = "url is required" });
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return BadRequest(new { error = "Invalid url" });
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Serve-document: upstream returned {StatusCode} for {Url}", response.StatusCode, url);
                return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Failed to load document" });
            }

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/pdf";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

            return File(bytes, contentType);
        }
        catch (TaskCanceledException)
        {
            return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Serve-document failed for {Url}", url);
            return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Failed to load document" });
        }
    }

    [HttpGet("library/{projectId}")]
    [ProducesResponseType(typeof(IEnumerable<ReaderLibraryBookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ReaderLibraryBookDto>>> GetReaderLibrary(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest(new { error = "projectId is required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var response = await _mediaServiceClient.ListReaderLibraryBooksAsync(
                projectId.Trim(),
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(response.Books.Select(MapReaderLibraryBookDto));
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid Reader library request" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while loading Reader library for project {ProjectId}", projectId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to load Reader library" });
        }
    }

    [HttpPut("library/{projectId}/books/{bookId}")]
    [ProducesResponseType(typeof(ReaderLibraryBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReaderLibraryBookDto>> SaveReaderLibraryBook(string projectId, string bookId, [FromBody] SaveReaderLibraryBookDto? book)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest(new { error = "projectId is required" });
        }

        if (string.IsNullOrWhiteSpace(bookId))
        {
            return BadRequest(new { error = "bookId is required" });
        }

        if (book == null)
        {
            return BadRequest(new { error = "Book payload is required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var currentUser = await _authorizationServiceClient.GetUserInfoAsync(userId, HttpContext.RequestAborted).ConfigureAwait(false);

            // Check Text Workspace limits
            var entitlements = await _billingServiceClient.GetEntitlementsAsync(userId, HttpContext.RequestAborted).ConfigureAwait(false);
            if (entitlements.Entitlements.TryGetValue("textWorkspaceMaxBooks", out var maxStr) && int.TryParse(maxStr, out var maxBooks) && maxBooks != -1)
            {
                var library = await _mediaServiceClient.ListReaderLibraryBooksAsync(projectId.Trim(), userId, roles, HttpContext.RequestAborted).ConfigureAwait(false);
                var isNew = !library.Books.Any(b => string.Equals(b.Id, bookId, StringComparison.OrdinalIgnoreCase));
                if (isNew && library.Books.Count >= maxBooks)
                {
                    return BadRequest(new { error = $"Free plan limit reached. You can only have {maxBooks} self-made books. Please upgrade your plan." });
                }
            }

            var response = await _mediaServiceClient.SaveReaderLibraryBookAsync(
                new SaveReaderLibraryBookRequest
                {
                    ProjectId = projectId.Trim(),
                    Book = new ReaderLibraryBook
                    {
                        Id = bookId.Trim(),
                        Title = book.Title ?? string.Empty,
                        FileName = book.FileName ?? string.Empty,
                        DocumentId = book.DocumentId ?? string.Empty,
                        PageCount = book.PageCount ?? 0,
                        UploadedAt = book.UploadedAt ?? string.Empty,
                        LastOpenedAt = book.LastOpenedAt ?? string.Empty,
                        LastPageNumber = book.LastReadPage ?? 0,
                        CollectionId = book.CollectionId ?? string.Empty,
                        CollectionName = book.CollectionName ?? string.Empty,
                        OwnerUserId = currentUser.Id,
                        OwnerUserName = currentUser.UserName ?? string.Empty,
                        OwnerEmail = currentUser.Email ?? string.Empty,
                        ReadingMode = string.IsNullOrWhiteSpace(book.ReadingMode) ? "pdf" : book.ReadingMode.Trim(),
                        HasExtractedText = book.HasExtractedText ?? false,
                        CoverImageUrl = book.CoverImageUrl ?? string.Empty,
                        AudioUrl = book.AudioUrl ?? string.Empty,
                        CefrLevel = book.CefrLevel ?? string.Empty,
                        Summary = book.Summary ?? string.Empty
                    }
                },
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(MapReaderLibraryBookDto(response.Book));
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid Reader library book" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while saving Reader library book {BookId} for project {ProjectId}", bookId, projectId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to save Reader library book" });
        }
    }

    [HttpDelete("library/{projectId}/books/{bookId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteReaderLibraryBook(string projectId, string bookId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest(new { error = "projectId is required" });
        }

        if (string.IsNullOrWhiteSpace(bookId))
        {
            return BadRequest(new { error = "bookId is required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            await _mediaServiceClient.DeleteReaderLibraryBookAsync(
                projectId.Trim(),
                bookId.Trim(),
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return NoContent();
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid Reader library delete request" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while deleting Reader library book {BookId} for project {ProjectId}", bookId, projectId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to delete Reader library book" });
        }
    }

    [HttpGet("library/{projectId}/collections")]
    [ProducesResponseType(typeof(IEnumerable<ReaderCollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReaderCollectionDto>>> GetReaderCollections(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest(new { error = "projectId is required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var response = await _mediaServiceClient.ListReaderCollectionsAsync(
                projectId.Trim(),
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(response.Collections.Select(MapReaderCollectionDto));
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid Reader collections request" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while loading Reader collections for project {ProjectId}", projectId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to load Reader collections" });
        }
    }

    [HttpPost("library/{projectId}/collections")]
    [ProducesResponseType(typeof(ReaderCollectionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReaderCollectionDto>> SaveReaderCollection(string projectId, [FromBody] SaveReaderCollectionDto? collection)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return BadRequest(new { error = "projectId is required" });
        }

        if (collection == null)
        {
            return BadRequest(new { error = "Collection payload is required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var currentUser = await _authorizationServiceClient.GetUserInfoAsync(userId, HttpContext.RequestAborted).ConfigureAwait(false);

            var response = await _mediaServiceClient.SaveReaderCollectionAsync(
                new SaveReaderCollectionRequest
                {
                    Collection = new ReaderCollection
                    {
                        Id = collection.Id,
                        ProjectId = projectId.Trim(),
                        Name = collection.Name ?? string.Empty,
                        Description = collection.Description ?? string.Empty,
                        OwnerUserId = currentUser.Id,
                        OwnerUserName = currentUser.UserName ?? string.Empty,
                        OwnerEmail = currentUser.Email ?? string.Empty
                    }
                },
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(MapReaderCollectionDto(response.Collection));
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid Reader collection" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while saving Reader collection for project {ProjectId}", projectId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to save Reader collection" });
        }
    }

    [HttpDelete("library/{projectId}/collections/{collectionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteReaderCollection(string projectId, string collectionId)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(collectionId))
        {
            return BadRequest(new { error = "projectId and collectionId are required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            await _mediaServiceClient.DeleteReaderCollectionAsync(
                projectId.Trim(),
                collectionId.Trim(),
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return NoContent();
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Invalid Reader collection delete request" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while deleting Reader collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to delete Reader collection" });
        }
    }

    [HttpPost("library/{projectId}/collections/{collectionId}/share")]
    [ProducesResponseType(typeof(ReaderCollectionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReaderCollectionDto>> ShareReaderCollection(string projectId, string collectionId, [FromBody] ShareReaderCollectionDto? payload)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(collectionId))
        {
            return BadRequest(new { error = "projectId and collectionId are required" });
        }

        if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var targetUser = await _authorizationServiceClient.FindUserByEmailAsync(payload.Email.Trim(), HttpContext.RequestAborted).ConfigureAwait(false);

            var response = await _mediaServiceClient.ShareReaderCollectionAsync(
                new ShareReaderCollectionRequest
                {
                    ProjectId = projectId.Trim(),
                    CollectionId = collectionId.Trim(),
                    Collaborator = new ReaderCollectionCollaborator
                    {
                        UserId = targetUser.Id,
                        UserName = targetUser.UserName ?? string.Empty,
                        Email = targetUser.Email ?? string.Empty,
                        CanEdit = payload.CanEdit
                    }
                },
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(MapReaderCollectionDto(response.Collection));
        }
        catch (RpcException ex) when (ex.StatusCode is Grpc.Core.StatusCode.InvalidArgument or Grpc.Core.StatusCode.NotFound)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Could not share Reader collection" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while sharing Reader collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to share Reader collection" });
        }
    }

    [HttpDelete("library/{projectId}/collections/{collectionId}/share/{collaboratorUserId}")]
    [ProducesResponseType(typeof(ReaderCollectionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReaderCollectionDto>> UnshareReaderCollection(string projectId, string collectionId, string collaboratorUserId)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(collectionId) || string.IsNullOrWhiteSpace(collaboratorUserId))
        {
            return BadRequest(new { error = "projectId, collectionId and collaboratorUserId are required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var response = await _mediaServiceClient.UnshareReaderCollectionAsync(
                new UnshareReaderCollectionRequest
                {
                    ProjectId = projectId.Trim(),
                    CollectionId = collectionId.Trim(),
                    CollaboratorUserId = collaboratorUserId.Trim()
                },
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(MapReaderCollectionDto(response.Collection));
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            return BadRequest(new { error = ex.Status.Detail ?? "Could not unshare Reader collection" });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while unsharing Reader collection {CollectionId}", collectionId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to unshare Reader collection" });
        }
    }

    [HttpGet("library/shared-collections")]
    [ProducesResponseType(typeof(IEnumerable<ReaderCollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReaderCollectionDto>>> GetSharedReaderCollections()
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var response = await _mediaServiceClient.ListSharedReaderCollectionsAsync(
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(response.Collections.Select(MapReaderCollectionDto));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while loading shared Reader collections");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Status.Detail ?? "Failed to load shared Reader collections" });
        }
    }

    /// <summary>
    /// Отдаёт изображение по ID (после загрузки) или проксирует по URL (для существующих карточек).
    /// </summary>
    [HttpGet("serve-image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ServeImage([FromQuery] string? id, [FromQuery] string? url, CancellationToken cancellationToken = default)
    {
        string? targetUrl = null;

        if (!string.IsNullOrEmpty(id))
        {
            try
            {
                var userId = MappingHelper.GetUserId(User, Request.Headers);
                var roles = MappingHelper.GetRoles(User, Request.Headers);
                var response = await _mediaServiceClient.GetImageUrlAsync(id, userId, roles, cancellationToken).ConfigureAwait(false);
                targetUrl = response.Url;
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument || ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                return NotFound(new { error = "Image not found" });
            }
        }
        else if (!string.IsNullOrEmpty(url))
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return BadRequest(new { error = "Invalid url" });
            }

            targetUrl = url;
        }
        else
        {
            return BadRequest(new { error = "id or url is required" });
        }

        if (string.IsNullOrEmpty(targetUrl))
        {
            return NotFound(new { error = "Image not found" });
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync(targetUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Serve-image: upstream returned {StatusCode} for {Url}", response.StatusCode, targetUrl);
                return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Failed to load image" });
            }

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

            return File(bytes, contentType);
        }
        catch (TaskCanceledException)
        {
            return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Serve-image failed for {Url}", targetUrl);
            return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Failed to load image" });
        }
    }

    private static ReaderLibraryBookDto MapReaderLibraryBookDto(ReaderLibraryBook book)
    {
        return new ReaderLibraryBookDto
        {
            Id = book.Id,
            Title = book.Title,
            FileName = book.FileName,
            Url = book.Url,
            DocumentId = string.IsNullOrWhiteSpace(book.DocumentId) ? null : book.DocumentId,
            PageCount = book.PageCount > 0 ? book.PageCount : null,
            UploadedAt = book.UploadedAt,
            LastOpenedAt = string.IsNullOrWhiteSpace(book.LastOpenedAt) ? null : book.LastOpenedAt,
            LastReadPage = book.LastPageNumber > 0 ? book.LastPageNumber : null,
            CollectionId = string.IsNullOrWhiteSpace(book.CollectionId) ? null : book.CollectionId,
            CollectionName = string.IsNullOrWhiteSpace(book.CollectionName) ? null : book.CollectionName,
            IsShared = book.IsShared,
            OwnerUserId = string.IsNullOrWhiteSpace(book.OwnerUserId) ? null : book.OwnerUserId,
            OwnerUserName = string.IsNullOrWhiteSpace(book.OwnerUserName) ? null : book.OwnerUserName,
            OwnerEmail = string.IsNullOrWhiteSpace(book.OwnerEmail) ? null : book.OwnerEmail,
            ReadingMode = string.IsNullOrWhiteSpace(book.ReadingMode) ? "pdf" : book.ReadingMode,
            HasExtractedText = book.HasExtractedText,
            CoverImageUrl = string.IsNullOrWhiteSpace(book.CoverImageUrl) ? null : book.CoverImageUrl,
            AudioUrl = string.IsNullOrWhiteSpace(book.AudioUrl) ? null : book.AudioUrl,
            CefrLevel = string.IsNullOrWhiteSpace(book.CefrLevel) ? null : book.CefrLevel,
            Summary = string.IsNullOrWhiteSpace(book.Summary) ? null : book.Summary
        };
    }

    private static ReaderCollectionDto MapReaderCollectionDto(ReaderCollection collection)
    {
        return new ReaderCollectionDto
        {
            Id = collection.Id,
            ProjectId = collection.ProjectId,
            Name = collection.Name,
            Description = string.IsNullOrWhiteSpace(collection.Description) ? null : collection.Description,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
            OwnerUserId = collection.OwnerUserId,
            OwnerUserName = collection.OwnerUserName,
            OwnerEmail = collection.OwnerEmail,
            IsSharedWithMe = collection.IsSharedWithMe,
            CanEdit = collection.CanEdit,
            BookCount = collection.BookCount,
            Collaborators = collection.Collaborators.Select(item => new ReaderCollectionCollaboratorDto
            {
                UserId = item.UserId,
                UserName = item.UserName,
                Email = item.Email,
                CanEdit = item.CanEdit,
                SharedAt = item.SharedAt
            }).ToList(),
            Books = collection.Books.Select(MapReaderLibraryBookDto).ToList()
        };
    }
}
