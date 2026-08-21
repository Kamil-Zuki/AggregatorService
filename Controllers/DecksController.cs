using AggregatorService.Dtos;
using AggregatorService.Helpers;
using AggregatorService.Options;
using AggregatorService.Services;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pvs.Content.Grpc;

namespace AggregatorService.Controllers;

/// <summary>
/// Контроллер для работы с колодами
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Требуем аутентификацию через JWT токен от authorization-module для всех методов
public class DecksController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyServiceClient;
    private readonly ILogger<DecksController> _logger;
    private readonly IMapper _mapper;
    private readonly FeaturesOptions _features;

    public DecksController(
        IVocabularyServiceClient vocabularyServiceClient,
        ILogger<DecksController> logger,
        IMapper mapper,
        IOptions<FeaturesOptions> features)
    {
        _vocabularyServiceClient = vocabularyServiceClient;
        _logger = logger;
        _mapper = mapper;
        _features = features.Value;
    }

    //===== SR-STR-03: Получение дерева колод =====
    /// <summary>
    /// Получает дерево колод для проекта (SR-STR-03)
    /// Альтернативный маршрут: /api/decks/tree/{projectId}
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <param name="libraryFilter">Фильтр библиотеки: Mine | Downloaded | Public (опционально)</param>
    /// <returns>Дерево колод</returns>
    [HttpGet("tree/{projectId}")]
    [ProducesResponseType(typeof(List<DeckTreeItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DeckTreeItemDto>>> GetDeckTree(string projectId, [FromQuery] string? libraryFilter = null)
    {
        return await GetDeckTreeInternal(projectId, libraryFilter);
    }


    /// <summary>
    /// Внутренний метод для получения дерева колод
    /// </summary>
    private async Task<ActionResult<List<DeckTreeItemDto>>> GetDeckTreeInternal(string projectId, string? libraryFilter = null)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetDeckTree request from user {UserId} for project {ProjectId}",
                userId,
                projectId);

            var grpcRequest = new GetDeckTreeRequest
            {
                ProjectId = projectId,
                LibraryFilter = ParseLibraryFilter(libraryFilter)
            };

            var grpcResponse = await _vocabularyServiceClient.GetDeckTreeAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDtos = grpcResponse.RootDecks
                .Select(d => _mapper.Map<DeckTreeItemDto>(d))
                .ToList();

            _logger.LogInformation(
                "Deck tree retrieved successfully for project {ProjectId}",
                projectId);

            return Ok(responseDtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting deck tree");
            
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
            _logger.LogError(ex, "Error getting deck tree");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>Парсит query-параметр libraryFilter в gRPC enum.</summary>
    public static LibraryFilter ParseLibraryFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return LibraryFilter.Unspecified;
        return value.Trim().ToUpperInvariant() switch
        {
            "MINE" => LibraryFilter.Mine,
            "DOWNLOADED" => LibraryFilter.Downloaded,
            "PUBLIC" => LibraryFilter.Public,
            _ => LibraryFilter.Unspecified
        };
    }

    private static ContributionPolicyDto ParseContributionPolicy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ContributionPolicyDto.Closed;
        return value.Trim().ToUpperInvariant() switch
        {
            "OPEN" => ContributionPolicyDto.Open,
            "RESTRICTED" => ContributionPolicyDto.Restricted,
            "CLOSED" => ContributionPolicyDto.Closed,
            _ => ContributionPolicyDto.Closed
        };
    }

    private static LicenseTypeDto ParseLicenseType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return LicenseTypeDto.Private;
        return value.Trim().ToUpperInvariant() switch
        {
            "PRIVATE" => LicenseTypeDto.Private,
            "FREE_ATTRIBUTION" => LicenseTypeDto.FreeAttribution,
            "COMMERCIAL" => LicenseTypeDto.Commercial,
            "COMMERCIAL_DERIVATIVE" => LicenseTypeDto.CommercialDerivative,
            _ => LicenseTypeDto.Private
        };
    }

    //===== SR-VOC-01: Создание колоды =====
    /// <summary>
    /// Создает новую колоду (SR-VOC-01)
    /// </summary>
    /// <param name="request">Данные для создания колоды</param>
    /// <returns>Созданная колода</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DeckResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeckResponseDto>> CreateDeck([FromBody] CreateDeckDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "CreateDeck request from user {UserId} for project {ProjectId}",
                userId,
                request.ProjectId);

            // MVP: publish/community fields require EnableAdvancedModules
            if (!_features.EnableAdvancedModules)
            {
                request.IsPublic = false;
            }

            var grpcRequest = _mapper.Map<CreateDeckRequest>(request);

            var grpcResponse = await _vocabularyServiceClient.CreateDeckAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<DeckResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Deck {DeckId} created successfully for user {UserId}",
                responseDto.Id,
                userId);

            return CreatedAtAction(
                nameof(GetDeckById),
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
            _logger.LogError(ex, "gRPC error when creating deck");
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.FailedPrecondition => StatusCodes.Status412PreconditionFailed,
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deck");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-VOC-01: Обновление колоды =====
    /// <summary>
    /// Обновляет колоду (SR-VOC-01)
    /// </summary>
    /// <param name="id">Идентификатор колоды</param>
    /// <param name="request">Данные для обновления</param>
    /// <returns>Обновленная колода</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DeckResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeckResponseDto>> UpdateDeck(
        string id,
        [FromBody] UpdateDeckDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "UpdateDeck request from user {UserId} for deck {DeckId}",
                userId,
                id);

            // MVP: ignore publish/community field changes when advanced modules are off
            if (!_features.EnableAdvancedModules)
            {
                if (request.IsPublic == true)
                {
                    request.IsPublic = false;
                }

                request.ContributionPolicy = null;
            }

            var grpcRequest = _mapper.Map<UpdateDeckRequest>(request);
            grpcRequest.DeckId = id;

            if (!_features.EnableAdvancedModules)
            {
                // AutoMapper may still populate the protobuf oneof from a null DTO field — clear explicitly
                grpcRequest.ClearContributionPolicy();
                grpcRequest.ClearLicenseType();
            }

            var grpcResponse = await _vocabularyServiceClient.UpdateDeckAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<DeckResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Deck {DeckId} updated successfully by user {UserId}",
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
            _logger.LogError(ex, "gRPC error when updating deck");
            
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
            _logger.LogError(ex, "Error updating deck");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-BG-02: Удаление колоды =====
    /// <summary>
    /// Удаляет колоду (SR-BG-02)
    /// </summary>
    /// <param name="id">Идентификатор колоды</param>
    /// <returns>Пустой ответ</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDeck(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "DeleteDeck request from user {UserId} for deck {DeckId}",
                userId,
                id);

            var grpcRequest = new DeleteDeckRequest
            {
                DeckId = id
            };

            await _vocabularyServiceClient.DeleteDeckAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            _logger.LogInformation(
                "Deck {DeckId} deleted successfully by user {UserId}",
                id,
                userId);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when deleting deck");
            
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
            _logger.LogError(ex, "Error deleting deck");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Получение детальной информации о колоде =====
    /// <summary>
    /// Получает детальную информацию о колоде по идентификатору (Id, Title, Description, Stats, ParentDeckId)
    /// </summary>
    /// <param name="id">Идентификатор колоды</param>
    /// <returns>Детальная информация о колоде со статистикой карточек</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DeckDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeckDetailDto>> GetDeckById(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetDeckById request from user {UserId} for deck {DeckId}",
                userId,
                id);

            var grpcRequest = new GetDeckDetailRequest
            {
                DeckId = id
            };

            var grpcResponse = await _vocabularyServiceClient.GetDeckDetailAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = new DeckDetailDto
            {
                Id = grpcResponse.Id,
                Title = grpcResponse.Title,
                Description = grpcResponse.Description,
                ParentDeckId = grpcResponse.ParentDeckId,
                ProjectId = grpcResponse.ProjectId ?? string.Empty,
                OwnerId = grpcResponse.OwnerId ?? string.Empty,
                CoverImageUrl = string.IsNullOrEmpty(grpcResponse.CoverImageUrl) ? null : grpcResponse.CoverImageUrl,
                IsPublic = grpcResponse.IsPublic,
                ContributionPolicy = ParseContributionPolicy(grpcResponse.ContributionPolicy),
                LicenseType = ParseLicenseType(grpcResponse.LicenseType),
                ForkedFromId = string.IsNullOrEmpty(grpcResponse.ForkedFromId) ? null : grpcResponse.ForkedFromId,
                CreatedAt = grpcResponse.CreatedAt?.ToDateTime() ?? DateTime.MinValue,
                CardCount = grpcResponse.CardCount,
                Stats = new DeckDetailStatsDto
                {
                    NewCardsCount = grpcResponse.Stats.NewCardsCount,
                    LearningCardsCount = grpcResponse.Stats.LearningCardsCount,
                    DueCardsCount = grpcResponse.Stats.DueCardsCount,
                    StudyableNowCount = grpcResponse.Stats.StudyableNowCount,
                    TotalCardsCount = grpcResponse.Stats.TotalCardsCount
                }
            };

            _logger.LogInformation(
                "Deck {DeckId} retrieved successfully for user {UserId}",
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
            _logger.LogError(ex, "gRPC error when getting deck detail");

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
            _logger.LogError(ex, "Error getting deck detail");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }
}

