using AggregatorService.Dtos;
using AggregatorService.Helpers;
using AggregatorService.Services;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pvs.Content.Grpc;

namespace AggregatorService.Controllers;

/// <summary>
/// Контроллер для работы с карточками
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Требуем аутентификацию через JWT токен от authorization-module для всех методов
public class CardsController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyServiceClient;
    private readonly IMediaServiceClient _mediaServiceClient;
    private readonly ILogger<CardsController> _logger;
    private readonly IMapper _mapper;

    public CardsController(
        IVocabularyServiceClient vocabularyServiceClient,
        IMediaServiceClient mediaServiceClient,
        ILogger<CardsController> logger,
        IMapper mapper)
    {
        _vocabularyServiceClient = vocabularyServiceClient;
        _mediaServiceClient = mediaServiceClient;
        _logger = logger;
        _mapper = mapper;
    }

    //===== SR-VOC-01: Создание карточки вручную =====
    /// <summary>
    /// Создает новую карточку вручную (SR-VOC-01)
    /// </summary>
    /// <param name="request">Данные для создания карточки</param>
    /// <returns>Созданная карточка</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CardResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CardResponseDto>> CreateCard([FromBody] CreateCardDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "CreateCard request from user {UserId} for deck {DeckId}",
                userId,
                request.DeckId);

            var grpcRequest = _mapper.Map<CreateCardRequest>(request);

            var grpcResponse = await _vocabularyServiceClient.CreateCardAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<CardResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Card {CardId} created successfully for user {UserId}",
                responseDto.Id,
                userId);

            return CreatedAtAction(
                nameof(GetCard),
                new { id = responseDto.Id },
                responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating card");
            
            if (AggregatorService.Helpers.BillingLimitHttp.TryHandleRpcException(ex, out var limitResult))
            {
                return limitResult;
            }
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    [HttpPost("check-duplicates")]
    [ProducesResponseType(typeof(CheckCardDuplicatesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CheckCardDuplicatesResponseDto>> CheckDuplicates([FromBody] CheckCardDuplicatesRequestDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = _mapper.Map<CheckCardDuplicatesRequest>(request);
            var grpcResponse = await _vocabularyServiceClient.CheckCardDuplicatesAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(_mapper.Map<CheckCardDuplicatesResponseDto>(grpcResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized duplicate check attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when checking duplicate cards");

            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate cards");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-API-01: Захват карточки =====
    /// <summary>
    /// Захватывает карточку из внешнего источника (SR-API-01)
    /// </summary>
    /// <param name="request">Данные для захвата карточки</param>
    /// <returns>Созданная карточка</returns>
    [HttpPost("capture")]
    [ProducesResponseType(typeof(CardResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CardResponseDto>> CaptureCard([FromBody] CaptureCardDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "CaptureCard request from user {UserId} for project {ProjectId}",
                userId,
                request.ProjectId);

            var grpcRequest = _mapper.Map<CaptureCardRequest>(request);

            var grpcResponse = await _vocabularyServiceClient.CaptureCardAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<CardResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Card {CardId} captured successfully for user {UserId}",
                responseDto.Id,
                userId);

            return CreatedAtAction(
                nameof(GetCard),
                new { id = responseDto.Id },
                responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when capturing card");
            
            if (AggregatorService.Helpers.BillingLimitHttp.TryHandleRpcException(ex, out var limitResult))
            {
                return limitResult;
            }
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                Grpc.Core.StatusCode.ResourceExhausted => StatusCodes.Status413PayloadTooLarge,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing card");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-SRC-01: Полнотекстовый поиск =====
    /// <summary>
    /// Выполняет полнотекстовый поиск по карточкам (SR-SRC-01)
    /// </summary>
    /// <param name="query">Поисковый запрос</param>
    /// <param name="projectId">Идентификатор проекта (опционально)</param>
    /// <param name="deckId">Идентификатор колоды (опционально)</param>
    /// <param name="pageNumber">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="srsStatuses">Фильтр по статусам SRS (опционально)</param>
    /// <returns>Список найденных карточек с пагинацией</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedResponseDto<CardResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponseDto<CardResponseDto>>> SearchCards(
        [FromQuery] string query = "",
        [FromQuery] string? projectId = null,
        [FromQuery] string? deckId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string[]? srsStatuses = null)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var queryValue = query ?? string.Empty;

            _logger.LogInformation(
                "SearchCards request from user {UserId}, query: {Query}",
                userId,
                queryValue);

            var grpcRequest = new SearchCardsRequest
            {
                Query = queryValue,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (!string.IsNullOrEmpty(projectId))
            {
                grpcRequest.ProjectId = projectId;
            }

            if (!string.IsNullOrEmpty(deckId))
            {
                grpcRequest.DeckId = deckId;
            }

            if (srsStatuses != null && srsStatuses.Length > 0)
            {
                foreach (var status in srsStatuses)
                {
                    if (Enum.TryParse<SrsStatus>(status, true, out var srsStatus))
                    {
                        grpcRequest.SrsStatuses.Add(srsStatus);
                    }
                }
            }

            var grpcResponse = await _vocabularyServiceClient.SearchCardsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = new PaginatedResponseDto<CardResponseDto>
            {
                Items = grpcResponse.Items.Select(c => _mapper.Map<CardResponseDto>(c)).ToList(),
                PageNumber = grpcResponse.PageNumber,
                TotalPages = grpcResponse.TotalPages,
                TotalCount = grpcResponse.TotalCount,
                HasPreviousPage = grpcResponse.PageNumber > 1,
                HasNextPage = grpcResponse.PageNumber < grpcResponse.TotalPages
            };

            _logger.LogInformation(
                "SearchCards completed for user {UserId}, found {Count} cards",
                userId,
                responseDto.TotalCount);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when searching cards");
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cards");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Тип заметки Anki-like и шаблоны карточек для редактора (динамические поля).
    /// </summary>
    [HttpGet("note-type/editor")]
    [ProducesResponseType(typeof(GetNoteTypeForEditorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetNoteTypeForEditorResponseDto>> GetNoteTypeForEditor([FromQuery] string projectId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(projectId) || !Guid.TryParse(projectId, out _))
                return BadRequest(new { error = "Invalid projectId" });

            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new GetNoteTypeForEditorRequest { ProjectId = projectId };
            var grpcResponse = await _vocabularyServiceClient.GetNoteTypeForEditorAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(_mapper.Map<GetNoteTypeForEditorResponseDto>(grpcResponse));
        }
        catch (Grpc.Core.RpcException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetNoteTypeForEditor failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Получение карточки =====
    /// <summary>
    /// Получает карточку по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор карточки</param>
    /// <returns>Информация о карточке</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CardResponseDto>> GetCard(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetCard request from user {UserId} for card {CardId}",
                userId,
                id);

            var grpcRequest = new GetCardRequest
            {
                CardId = id
            };

            var grpcResponse = await _vocabularyServiceClient.GetCardAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<CardResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Card {CardId} retrieved successfully for user {UserId}",
                id,
                userId);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting card");
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting card");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-VOC-02: Редактирование карточки =====
    /// <summary>
    /// Обновляет карточку (SR-VOC-02)
    /// </summary>
    /// <param name="id">Идентификатор карточки</param>
    /// <param name="request">Данные для обновления</param>
    /// <returns>Обновленная карточка</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CardResponseDto>> UpdateCard(
        string id,
        [FromBody] UpdateCardDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "UpdateCard request from user {UserId} for card {CardId}",
                userId,
                id);

            var grpcRequest = _mapper.Map<UpdateCardRequest>(request);
            grpcRequest.CardId = id;

            var grpcResponse = await _vocabularyServiceClient.UpdateCardAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<CardResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Card {CardId} updated successfully by user {UserId}",
                id,
                userId);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating card");
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating card");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-VOC-06: Массовый импорт =====
    /// <summary>
    /// Массовое создание карточек (SR-VOC-06)
    /// </summary>
    /// <param name="request">Данные для массового создания</param>
    /// <returns>Список созданных карточек</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(List<CardResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CardResponseDto>>> BulkCreateCards([FromBody] BulkCreateCardsDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "BulkCreateCards request from user {UserId} for deck {DeckId}, count: {Count}",
                userId,
                request.DeckId,
                request.Cards.Count);

            var grpcRequest = _mapper.Map<BulkCreateCardsRequest>(request);

            var grpcResponse = await _vocabularyServiceClient.BulkCreateCardsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDtos = grpcResponse.CreatedCards
                .Select(c => _mapper.Map<CardResponseDto>(c))
                .ToList();

            _logger.LogInformation(
                "BulkCreateCards completed for user {UserId}, created {Count} cards",
                userId,
                responseDtos.Count);

            return Ok(responseDtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when bulk creating cards");
            
            if (AggregatorService.Helpers.BillingLimitHttp.TryHandleRpcException(ex, out var limitResult))
            {
                return limitResult;
            }
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating cards");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Фоновый импорт из файла =====
    /// <summary>
    /// Массовое создание карточек через фоновую задачу (SR-VOC-06)
    /// </summary>
    [HttpPost("import-file")]
    [RequestSizeLimit(500L * 1024 * 1024)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 500L * 1024 * 1024)] // 500 MB
    public async Task<ActionResult> ImportFile([FromForm] IFormFile file, [FromForm] string config, [FromForm] Guid deckId)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation("ImportFile request from user {UserId} for deck {DeckId}", userId, deckId);

            // Upload document to MediaService first
            using var stream = file.OpenReadStream();
            var docResponse = await _mediaServiceClient.UploadDocumentAsync(new Pvs.Media.Grpc.UploadDocumentRequest
            {
                DocumentData = Google.Protobuf.ByteString.FromStream(stream),
                ContentType = file.ContentType,
                FileName = file.FileName
            }, userId, roles, cancellationToken: HttpContext.RequestAborted);

            var grpcRequest = new StartImportJobRequest
            {
                DeckId = deckId.ToString(),
                DocumentId = docResponse.DocumentId,
                FileName = file.FileName,
                ConfigJson = config ?? "{}"
            };

            var grpcResponse = await _vocabularyServiceClient.StartImportJobAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(new { jobId = grpcResponse.JobId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting import job");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    [HttpGet("import-job/{jobId}")]
    public async Task<ActionResult> GetImportJobStatus(string jobId)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcResponse = await _vocabularyServiceClient.GetImportJobStatusAsync(
                new GetImportJobStatusRequest { JobId = jobId },
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(new
            {
                jobId = grpcResponse.JobId,
                status = grpcResponse.Status,
                totalRows = grpcResponse.TotalRows,
                processedRows = grpcResponse.ProcessedRows,
                errorMessage = grpcResponse.ErrorMessage
            });
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(new { error = "Job not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import job status");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Удаление карточки =====
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCard(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new DeleteCardRequest { CardId = id };
            await _vocabularyServiceClient.DeleteCardAsync(grpcRequest, userId, roles, HttpContext.RequestAborted);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized delete attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Массовое удаление =====
    [HttpPost("bulk-delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkDeleteCards([FromBody] BulkCardIdsRequestDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new BulkDeleteCardsRequest();
            grpcRequest.CardIds.AddRange(request.CardIds);

            await _vocabularyServiceClient.BulkDeleteCardsAsync(grpcRequest, userId, roles, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting cards");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Перемещение карточек =====
    [HttpPost("move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MoveCards([FromBody] MoveCardsRequestDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new MoveCardsRequest
            {
                DeckId = request.DeckId
            };
            grpcRequest.CardIds.AddRange(request.CardIds);

            await _vocabularyServiceClient.MoveCardsAsync(grpcRequest, userId, roles, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving cards");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Сброс прогресса =====
    [HttpPost("bulk-reset-progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkResetProgress([FromBody] BulkCardIdsRequestDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new ResetCardProgressRequest();
            grpcRequest.CardIds.AddRange(request.CardIds);

            await _vocabularyServiceClient.ResetCardProgressAsync(grpcRequest, userId, roles, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting card progress");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Список leech-карточек =====
    [HttpGet("leeches")]
    [ProducesResponseType(typeof(PaginatedResponseDto<CardResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponseDto<CardResponseDto>>> GetLeechCards(
        [FromQuery] string projectId,
        [FromQuery] int threshold = 8,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new GetLeechCardsRequest
            {
                ProjectId = projectId,
                Threshold = threshold,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var grpcResponse = await _vocabularyServiceClient.GetLeechCardsAsync(grpcRequest, userId, roles, HttpContext.RequestAborted);

            return Ok(new PaginatedResponseDto<CardResponseDto>
            {
                Items = grpcResponse.Items.Select(c => _mapper.Map<CardResponseDto>(c)).ToList(),
                PageNumber = grpcResponse.PageNumber,
                TotalPages = grpcResponse.TotalPages,
                TotalCount = grpcResponse.TotalCount,
                HasPreviousPage = grpcResponse.PageNumber > 1,
                HasNextPage = grpcResponse.PageNumber < grpcResponse.TotalPages
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leech cards");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Список карточек без медиа =====
    [HttpGet("missing-media")]
    [ProducesResponseType(typeof(PaginatedResponseDto<CardResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponseDto<CardResponseDto>>> GetCardsMissingMedia(
        [FromQuery] string projectId,
        [FromQuery] string? mediaType = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new GetCardsMissingMediaRequest
            {
                ProjectId = projectId,
                MediaType = mediaType ?? string.Empty,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var grpcResponse = await _vocabularyServiceClient.GetCardsMissingMediaAsync(grpcRequest, userId, roles, HttpContext.RequestAborted);

            return Ok(new PaginatedResponseDto<CardResponseDto>
            {
                Items = grpcResponse.Items.Select(c => _mapper.Map<CardResponseDto>(c)).ToList(),
                PageNumber = grpcResponse.PageNumber,
                TotalPages = grpcResponse.TotalPages,
                TotalCount = grpcResponse.TotalCount,
                HasPreviousPage = grpcResponse.PageNumber > 1,
                HasNextPage = grpcResponse.PageNumber < grpcResponse.TotalPages
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cards missing media");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }
}
