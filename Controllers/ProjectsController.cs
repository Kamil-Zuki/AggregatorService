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
/// Контроллер для работы с проектами
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Требуем аутентификацию через JWT токен от authorization-module для всех методов
public class ProjectsController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyServiceClient;
    private readonly IMediaServiceClient? _mediaServiceClient;
    private readonly ILogger<ProjectsController> _logger;
    private readonly IMapper _mapper;

    public ProjectsController(
        IVocabularyServiceClient vocabularyServiceClient,
        ILogger<ProjectsController> logger,
        IMapper mapper,
        IMediaServiceClient? mediaServiceClient = null)
    {
        _vocabularyServiceClient = vocabularyServiceClient;
        _logger = logger;
        _mapper = mapper;
        _mediaServiceClient = mediaServiceClient;
    }

    //===== SR-STR-01: Создание проекта =====
    /// <summary>
    /// Создает новый языковой проект (SR-STR-01)
    /// </summary>
    /// <param name="request">Данные для создания проекта</param>
    /// <returns>Созданный проект</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProjectResponseDto>> CreateProject([FromBody] CreateProjectDto request)
    {
        try
        {
            // Извлекаем user_id и roles из JWT токена (Claims из authorization-module)
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "CreateProject request from user {UserId} with roles: {Roles}",
                userId,
                string.Join(", ", roles));

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = _mapper.Map<CreateProjectRequest>(request);
            grpcRequest.UserId = userId.ToString();

            // Вызываем VocabularyService через gRPC
            var grpcResponse = await _vocabularyServiceClient.CreateProjectAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            // Преобразуем gRPC ответ в DTO
            var responseDto = _mapper.Map<ProjectResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Project {ProjectId} created successfully for user {UserId}",
                responseDto.Id,
                userId);

            return CreatedAtAction(
                nameof(GetProject),
                new { id = responseDto.Id },
                responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating project");
            
            if (AggregatorService.Helpers.BillingLimitHttp.TryHandleRpcException(ex, out var limitResult))
            {
                return limitResult;
            }
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.AlreadyExists => StatusCodes.Status409Conflict,
                Grpc.Core.StatusCode.ResourceExhausted => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-STR-01: Получение списка проектов =====
    /// <summary>
    /// Получает список всех проектов пользователя с краткой статистикой (SR-STR-01)
    /// </summary>
    /// <param name="includeArchived">Флаг включения архивных проектов</param>
    /// <returns>Список проектов пользователя</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ProjectResponseDto>>> GetProjects([FromQuery] bool includeArchived = false)
    {
        try
        {
            // Извлекаем user_id и roles из JWT токена (Claims из authorization-module)
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetProjects request from user {UserId} with roles: {Roles}, includeArchived: {IncludeArchived}",
                userId,
                string.Join(", ", roles),
                includeArchived);

            // Преобразуем параметры в gRPC запрос
            var grpcRequest = new GetProjectsRequest
            {
                IncludeArchived = includeArchived
            };
            grpcRequest.UserId = userId.ToString();

            // Вызываем VocabularyService через gRPC
            var grpcResponse = await _vocabularyServiceClient.GetProjectsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            // Преобразуем gRPC ответ в список DTO
            var responseDtos = grpcResponse.Projects
                .Select(p => _mapper.Map<ProjectResponseDto>(p))
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} projects for user {UserId}",
                responseDtos.Count,
                userId);

            return Ok(responseDtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting projects");
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-STR-02: Получение деталей проекта =====
    /// <summary>
    /// Получает проект по идентификатору с настройками FSRS (SR-STR-02)
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    /// <returns>Детали проекта</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProjectResponseDto>> GetProject(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetProject request from user {UserId} for project {ProjectId}",
                userId,
                id);

            var grpcRequest = new GetProjectDetailsRequest
            {
                ProjectId = id
            };

            var grpcResponse = await _vocabularyServiceClient.GetProjectDetailsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ProjectResponseDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting project");
            
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
            _logger.LogError(ex, "Error getting project");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-STR-02: Обновление настроек проекта =====
    /// <summary>
    /// Обновляет метаданные и настройки проекта (SR-STR-02)
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    /// <param name="request">Данные для обновления</param>
    /// <returns>Обновленный проект</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProjectResponseDto>> UpdateProject(
        string id,
        [FromBody] UpdateProjectDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "UpdateProject request from user {UserId} for project {ProjectId}",
                userId,
                id);

            var grpcRequest = _mapper.Map<UpdateProjectRequest>(request);
            grpcRequest.UserId = userId.ToString();
            grpcRequest.ProjectId = id;

            var grpcResponse = await _vocabularyServiceClient.UpdateProjectAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ProjectResponseDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating project");
            
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
            _logger.LogError(ex, "Error updating project");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-STR-03: Получение дерева колод =====
    /// <summary>
    /// Получает дерево колод для проекта (SR-STR-03)
    /// Маршрут согласно документации: /api/projects/{projectId}/decks/tree
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <param name="libraryFilter">Фильтр библиотеки: Mine | Downloaded | Public (опционально)</param>
    /// <returns>Дерево колод</returns>
    [HttpGet("{projectId}/decks/tree")]
    [ProducesResponseType(typeof(List<DeckTreeItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DeckTreeItemDto>>> GetDeckTree(string projectId, [FromQuery] string? libraryFilter = null)
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
                LibraryFilter = DecksController.ParseLibraryFilter(libraryFilter)
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

    //===== SR-STR-02: Удаление проекта =====
    /// <summary>
    /// Безвозвратно удаляет проект, его колоды, карточки, слова и медиафайлы (SR-STR-02)
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProject(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "DeleteProject request from user {UserId} for project {ProjectId}",
                userId,
                id);

            // 1. Очищаем медиаданные проекта в MediaService (S3 книги, коллекции, выжимки)
            if (_mediaServiceClient != null)
            {
                try
                {
                    await _mediaServiceClient.DeleteProjectMediaAsync(
                        new Pvs.Media.Grpc.DeleteProjectMediaRequest { ProjectId = id, UserId = userId.ToString() },
                        userId,
                        roles,
                        HttpContext.RequestAborted);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deleting project media for project {ProjectId}", id);
                }
            }

            // 2. Удаляем сущность проекта и каскадно связанные записи в VocabularyService
            var grpcRequest = new DeleteProjectRequest
            {
                ProjectId = id,
                UserId = userId.ToString()
            };

            await _vocabularyServiceClient.DeleteProjectAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            _logger.LogInformation(
                "Project {ProjectId} deleted successfully by user {UserId}",
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
            _logger.LogError(ex, "gRPC error when deleting project");

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
            _logger.LogError(ex, "Error deleting project");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }
}

