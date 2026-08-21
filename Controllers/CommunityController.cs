using AggregatorService.Dtos;
using AggregatorService.Dtos.Community;
using AggregatorService.Helpers;
using AggregatorService.Services;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pvs.Content.Grpc;

namespace AggregatorService.Controllers;

/// <summary>
/// Контроллер для работы с коллаборацией и маркетплейсом
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
[AggregatorService.Filters.FeatureFlagFilter("EnableAdvancedModules")]
public class CommunityController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyServiceClient;
    private readonly ILogger<CommunityController> _logger;
    private readonly IMapper _mapper;

    public CommunityController(
        IVocabularyServiceClient vocabularyServiceClient,
        ILogger<CommunityController> logger,
        IMapper mapper)
    {
        _vocabularyServiceClient = vocabularyServiceClient;
        _logger = logger;
        _mapper = mapper;
    }

    // ============================================================================
    // Contributions (SR-COL-01 до SR-COL-08)
    // ============================================================================

    //===== SR-COL-01: Создание предложения =====
    /// <summary>
    /// Создает предложение (SR-COL-01)
    /// </summary>
    [HttpPost("contributions")]
    [ProducesResponseType(typeof(ContributionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContributionResponseDto>> CreateContribution([FromBody] CreateContributionDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "CreateContribution request from user {UserId} for deck {DeckId}, type {Type}",
                userId,
                request.DeckId,
                request.Type);

            var grpcRequest = _mapper.Map<CreateContributionRequest>(request);
            var grpcResponse = await _vocabularyServiceClient.CreateContributionAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            // Получаем созданное предложение для ответа
            var getRequest = new GetContributionRequest
            {
                ContributionId = grpcResponse.ContributionId
            };
            var contributionResponse = await _vocabularyServiceClient.GetContributionAsync(
                getRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ContributionResponseDto>(contributionResponse.Contribution);

            return CreatedAtAction(
                nameof(GetContribution),
                new { id = responseDto.Id },
                responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to create contribution");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating contribution");
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
            _logger.LogError(ex, "Error creating contribution");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-COL-03: Получение списка предложений =====
    /// <summary>
    /// Получает список предложений (SR-COL-03)
    /// </summary>
    [HttpGet("decks/{deckId}/contributions")]
    [HttpGet("contributions")] // Альтернативный маршрут для "мои предложения"
    [ProducesResponseType(typeof(PaginatedResponseDto<ContributionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponseDto<ContributionResponseDto>>> GetContributions(
        [FromRoute] Guid? deckId = null,
        [FromQuery] string? status = null,
        [FromQuery] string role = "MODERATOR")
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetContributions request from user {UserId}, deckId: {DeckId}, status: {Status}, role: {Role}",
                userId,
                deckId?.ToString() ?? "all",
                status ?? "all",
                role);

            var grpcRequest = new GetContributionsRequest
            {
                Role = role
            };

            if (deckId.HasValue)
            {
                grpcRequest.DeckId = deckId.Value.ToString();
            }

            if (!string.IsNullOrEmpty(status))
            {
                grpcRequest.Status = status;
            }

            var grpcResponse = await _vocabularyServiceClient.GetContributionsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var contributions = grpcResponse.Contributions
                .Select(c => _mapper.Map<ContributionResponseDto>(c))
                .ToList();

            var response = new PaginatedResponseDto<ContributionResponseDto>
            {
                Items = contributions,
                TotalCount = contributions.Count,
                PageNumber = 1,
                TotalPages = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get contributions");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting contributions");
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
            _logger.LogError(ex, "Error getting contributions");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-COL-03: Просмотр различий (Diff) =====
    /// <summary>
    /// Получает предложение с различиями (SR-COL-03)
    /// </summary>
    [HttpGet("contributions/{id}")]
    [ProducesResponseType(typeof(ContributionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContributionResponseDto>> GetContribution(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetContribution request from user {UserId}, contribution {ContributionId}",
                userId,
                id);

            var grpcRequest = new GetContributionRequest
            {
                ContributionId = id
            };

            var grpcResponse = await _vocabularyServiceClient.GetContributionAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var contributionDto = _mapper.Map<ContributionResponseDto>(grpcResponse.Contribution);
            // Note: Diff information is available in grpcResponse.Diff but not included in ContributionResponseDto
            // If needed, create a separate endpoint or extend the DTO

            return Ok(contributionDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get contribution");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting contribution");
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
            _logger.LogError(ex, "Error getting contribution");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-COL-04: Принятие/Отклонение предложения =====
    /// <summary>
    /// Принимает или отклоняет предложение (SR-COL-04)
    /// </summary>
    [HttpPost("contributions/{id}/resolve")]
    [ProducesResponseType(typeof(ContributionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContributionResponseDto>> ResolveContribution(
        string id,
        [FromBody] ResolveContributionDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "ResolveContribution request from user {UserId}, contribution {ContributionId}, status {Status}",
                userId,
                id,
                request.Status);

            var grpcRequest = _mapper.Map<ResolveContributionRequest>(request);
            grpcRequest.ContributionId = id;

            var grpcResponse = await _vocabularyServiceClient.ResolveContributionAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            // Получаем обновленное предложение
            var getRequest = new GetContributionRequest
            {
                ContributionId = id
            };
            var contributionResponse = await _vocabularyServiceClient.GetContributionAsync(
                getRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ContributionResponseDto>(contributionResponse.Contribution);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to resolve contribution");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when resolving contribution");
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.FailedPrecondition => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving contribution");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-COL-06: Обновление политики вкладов =====
    /// <summary>
    /// Обновляет политику вкладов для колоды (SR-COL-06)
    /// </summary>
    [HttpPut("decks/{deckId}/contribution-policy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateContributionPolicy(
        Guid deckId,
        [FromBody] UpdateContributionPolicyDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "UpdateContributionPolicy request from user {UserId}, deck {DeckId}, policy {Policy}",
                userId,
                deckId,
                request.Policy);

            var grpcRequest = new UpdateContributionPolicyRequest
            {
                DeckId = deckId.ToString(),
                Policy = request.Policy
            };

            var grpcResponse = await _vocabularyServiceClient.UpdateContributionPolicyAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(new { success = grpcResponse.Success });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to update contribution policy");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating contribution policy");
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
            _logger.LogError(ex, "Error updating contribution policy");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    // ============================================================================
    // Publishing (SR-PUB-01 до SR-PUB-04)
    // ============================================================================

    //===== SR-PUB-01: Публикация колоды =====
    /// <summary>
    /// Публикует колоду (SR-PUB-01)
    /// </summary>
    [HttpPost("decks/{deckId}/publish")]
    [ProducesResponseType(typeof(PublishDeckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PublishDeckDto>> PublishDeck(
        Guid deckId,
        [FromBody] PublishDeckDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "PublishDeck request from user {UserId}, deck {DeckId}, license {LicenseType}",
                userId,
                deckId,
                request.LicenseType);

            request.DeckId = deckId;

            var grpcRequest = _mapper.Map<PublishDeckRequest>(request);
            var grpcResponse = await _vocabularyServiceClient.PublishDeckAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(new { deckId = Guid.Parse(grpcResponse.DeckId), isPublic = grpcResponse.IsPublic });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to publish deck");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when publishing deck");
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
            _logger.LogError(ex, "Error publishing deck");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-PUB-02: Клонирование колоды =====
    /// <summary>
    /// Клонирует колоду (SR-PUB-02)
    /// </summary>
    [HttpPost("decks/{deckId}/fork")]
    [ProducesResponseType(typeof(ForkDeckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForkDeckDto>> ForkDeck(
        Guid deckId,
        [FromBody] ForkDeckDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "ForkDeck request from user {UserId}, deck {DeckId}, targetProject {TargetProjectId}",
                userId,
                deckId,
                request.TargetProjectId);

            request.DeckId = deckId;

            var grpcRequest = _mapper.Map<ForkDeckRequest>(request);
            var grpcResponse = await _vocabularyServiceClient.ForkDeckAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(new
            {
                deckId = Guid.Parse(grpcResponse.DeckId),
                forkedFromId = grpcResponse.ForkedFromId != null ? Guid.Parse(grpcResponse.ForkedFromId) : (Guid?)null,
                licenseType = grpcResponse.LicenseType
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to fork deck");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when forking deck");
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
            _logger.LogError(ex, "Error forking deck");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-PUB-01: Получение опубликованных колод =====
    /// <summary>
    /// Получает список опубликованных колод (SR-PUB-01)
    /// </summary>
    [HttpGet("decks/published")]
    [ProducesResponseType(typeof(PaginatedResponseDto<PublishedDeckResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponseDto<PublishedDeckResponseDto>>> GetPublishedDecks(
        [FromQuery] Guid? authorId = null,
        [FromQuery] string? searchQuery = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetPublishedDecks request from user {UserId}, authorId: {AuthorId}, searchQuery: {SearchQuery}, page: {Page}",
                userId,
                authorId?.ToString() ?? "all",
                searchQuery ?? "none",
                page);

            var grpcRequest = new GetPublishedDecksRequest
            {
                Page = page,
                PageSize = pageSize
            };

            if (authorId.HasValue)
            {
                grpcRequest.AuthorId = authorId.Value.ToString();
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                grpcRequest.SearchQuery = searchQuery;
            }

            var grpcResponse = await _vocabularyServiceClient.GetPublishedDecksAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var decks = grpcResponse.Decks
                .Select(d => _mapper.Map<PublishedDeckResponseDto>(d))
                .ToList();

            var totalPages = (int)Math.Ceiling((double)grpcResponse.TotalCount / grpcResponse.PageSize);
            var response = new PaginatedResponseDto<PublishedDeckResponseDto>
            {
                Items = decks,
                TotalCount = grpcResponse.TotalCount,
                PageNumber = grpcResponse.Page,
                TotalPages = totalPages,
                HasPreviousPage = grpcResponse.Page > 1,
                HasNextPage = grpcResponse.Page < totalPages
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get published decks");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting published decks");
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
            _logger.LogError(ex, "Error getting published decks");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-PUB-04: Получение профиля автора =====
    /// <summary>
    /// Получает профиль автора (SR-PUB-04)
    /// </summary>
    [HttpGet("authors/{authorId}")]
    [ProducesResponseType(typeof(AuthorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthorProfileDto>> GetAuthorProfile(Guid authorId)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetAuthorProfile request from user {UserId}, author {AuthorId}",
                userId,
                authorId);

            var grpcRequest = new GetAuthorProfileRequest
            {
                AuthorId = authorId.ToString()
            };

            var grpcResponse = await _vocabularyServiceClient.GetAuthorProfileAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<AuthorProfileDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get author profile");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting author profile");
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
            _logger.LogError(ex, "Error getting author profile");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    // ============================================================================
    // Marketplace (SR-MKT-01 до SR-MKT-06)
    // ============================================================================

    //===== SR-MKT-01: Создание товара =====
    /// <summary>
    /// Создает товар (SR-MKT-01)
    /// </summary>
    [HttpPost("marketplace/products")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromBody] CreateProductDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "CreateProduct request from user {UserId}, deck {DeckId}, title {Title}",
                userId,
                request.DeckId,
                request.Title);

            var grpcRequest = _mapper.Map<CreateProductRequest>(request);
            var grpcResponse = await _vocabularyServiceClient.CreateProductAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            // Получаем созданный товар для ответа
            var getRequest = new GetProductDetailsRequest
            {
                ProductId = grpcResponse.ProductId
            };
            var productResponse = await _vocabularyServiceClient.GetProductDetailsAsync(
                getRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ProductResponseDto>(productResponse.Product);

            return CreatedAtAction(
                nameof(GetProductDetails),
                new { id = responseDto.Id },
                responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to create product");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating product");
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
            _logger.LogError(ex, "Error creating product");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-MKT-01: Обновление товара =====
    /// <summary>
    /// Обновляет товар (SR-MKT-01)
    /// </summary>
    [HttpPut("marketplace/products/{id}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductResponseDto>> UpdateProduct(
        string id,
        [FromBody] UpdateProductDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "UpdateProduct request from user {UserId}, product {ProductId}",
                userId,
                id);

            var grpcRequest = _mapper.Map<UpdateProductRequest>(request);
            grpcRequest.ProductId = id;

            var grpcResponse = await _vocabularyServiceClient.UpdateProductAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            // Получаем обновленный товар
            var getRequest = new GetProductDetailsRequest
            {
                ProductId = id
            };
            var productResponse = await _vocabularyServiceClient.GetProductDetailsAsync(
                getRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ProductResponseDto>(productResponse.Product);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to update product");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating product");
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
            _logger.LogError(ex, "Error updating product");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-MKT-01: Получение списка товаров =====
    /// <summary>
    /// Получает список товаров (SR-MKT-01)
    /// </summary>
    [HttpGet("marketplace/products")]
    [ProducesResponseType(typeof(PaginatedResponseDto<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponseDto<ProductResponseDto>>> GetProducts(
        [FromQuery] Guid? authorId = null,
        [FromQuery] string? searchQuery = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetProducts request from user {UserId}, authorId: {AuthorId}, searchQuery: {SearchQuery}, page: {Page}",
                userId,
                authorId?.ToString() ?? "all",
                searchQuery ?? "none",
                page);

            var grpcRequest = new GetProductsRequest
            {
                Page = page,
                PageSize = pageSize
            };

            if (authorId.HasValue)
            {
                grpcRequest.AuthorId = authorId.Value.ToString();
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                grpcRequest.SearchQuery = searchQuery;
            }

            if (minPrice.HasValue)
            {
                grpcRequest.MinPrice = (double)minPrice.Value;
            }

            if (maxPrice.HasValue)
            {
                grpcRequest.MaxPrice = (double)maxPrice.Value;
            }

            var grpcResponse = await _vocabularyServiceClient.GetProductsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var products = grpcResponse.Products
                .Select(p => _mapper.Map<ProductResponseDto>(p))
                .ToList();

            var totalPages = (int)Math.Ceiling((double)grpcResponse.TotalCount / grpcResponse.PageSize);
            var response = new PaginatedResponseDto<ProductResponseDto>
            {
                Items = products,
                TotalCount = grpcResponse.TotalCount,
                PageNumber = grpcResponse.Page,
                TotalPages = totalPages,
                HasPreviousPage = grpcResponse.Page > 1,
                HasNextPage = grpcResponse.Page < totalPages
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get products");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting products");
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
            _logger.LogError(ex, "Error getting products");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-MKT-02: Получение деталей товара =====
    /// <summary>
    /// Получает детали товара (SR-MKT-02)
    /// </summary>
    [HttpGet("marketplace/products/{id}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductResponseDto>> GetProductDetails(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetProductDetails request from user {UserId}, product {ProductId}",
                userId,
                id);

            var grpcRequest = new GetProductDetailsRequest
            {
                ProductId = id
            };

            var grpcResponse = await _vocabularyServiceClient.GetProductDetailsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ProductResponseDto>(grpcResponse.Product);
            // Note: Preview cards are available in grpcResponse.Preview but not included in ProductResponseDto
            // If needed, create a separate endpoint or extend the DTO

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get product details");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting product details");
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
            _logger.LogError(ex, "Error getting product details");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-MKT-05: Создание отзыва =====
    /// <summary>
    /// Создает отзыв (SR-MKT-05)
    /// </summary>
    [HttpPost("marketplace/products/{id}/reviews")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateReview(
        string id,
        [FromBody] CreateReviewDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "CreateReview request from user {UserId}, product {ProductId}, rating {Rating}",
                userId,
                id,
                request.Rating);

            request.ProductId = Guid.Parse(id);

            var grpcRequest = _mapper.Map<CreateReviewRequest>(request);
            var grpcResponse = await _vocabularyServiceClient.CreateReviewAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return CreatedAtAction(
                nameof(GetProductDetails),
                new { id },
                new { reviewId = Guid.Parse(grpcResponse.ReviewId) });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to create review");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating review");
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
            _logger.LogError(ex, "Error creating review");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-MKT-06: Получение статистики товара =====
    /// <summary>
    /// Получает статистику товара (SR-MKT-06)
    /// </summary>
    [HttpGet("marketplace/products/{id}/stats")]
    [ProducesResponseType(typeof(ProductStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductStatsDto>> GetProductStats(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetProductStats request from user {UserId}, product {ProductId}",
                userId,
                id);

            var grpcRequest = new GetProductStatsRequest
            {
                ProductId = id
            };

            var grpcResponse = await _vocabularyServiceClient.GetProductStatsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ProductStatsDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get product stats");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting product stats");
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
            _logger.LogError(ex, "Error getting product stats");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-MKT-03, SR-COL-07: Проверка прав доступа =====
    /// <summary>
    /// Проверяет права доступа к колоде (SR-MKT-03, SR-COL-07)
    /// </summary>
    [HttpGet("decks/{deckId}/entitlement")]
    [ProducesResponseType(typeof(EntitlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EntitlementDto>> CheckEntitlement(Guid deckId)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "CheckEntitlement request from user {UserId}, deck {DeckId}",
                userId,
                deckId);

            var grpcRequest = new CheckEntitlementRequest
            {
                DeckId = deckId.ToString()
            };

            var grpcResponse = await _vocabularyServiceClient.CheckEntitlementAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<EntitlementDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to check entitlement");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when checking entitlement");
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
            _logger.LogError(ex, "Error checking entitlement");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }
}
