using AggregatorService.Dtos;
using AggregatorService.Helpers;
using AggregatorService.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pvs.Content.Grpc;

namespace AggregatorService.Controllers;

/// <summary>
/// REST-мост к TermService (LingQ-style операции с терминами).
/// </summary>
[ApiController]
[Route("api/terms")]
[Authorize]
public class TermsController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabulary;
    private readonly ILogger<TermsController> _logger;

    public TermsController(IVocabularyServiceClient vocabulary, ILogger<TermsController> logger)
    {
        _vocabulary = vocabulary;
        _logger = logger;
    }

    /// <summary>Список терминов проекта (cursor-пагинация по UserTermStatus.UpdatedAt DESC, TermId ASC).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListProjectTermsResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListProjectTermsResponseDto>> List(
        [FromQuery] string projectId,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? q = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int? pageSize = null)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return BadRequest(new { error = "projectId is required" });

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var grpcRequest = TermGrpcMapper.ToListProjectTermsRequest(
                projectId.Trim(),
                status,
                type,
                q,
                pageNumber,
                pageSize,
                userId);
            var grpcResponse = await _vocabulary.ListProjectTermsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(TermGrpcMapper.ToListProjectTermsResponseDto(grpcResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListProjectTerms failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(TermDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TermDetailsDto>> CreateOrUpdate([FromBody] CreateOrUpdateTermDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var grpcRequest = TermGrpcMapper.ToCreateOrUpdateRequest(request, userId);
            var grpcResponse = await _vocabulary.CreateOrUpdateTermAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            var dto = TermGrpcMapper.ToTermDetailsDto(grpcResponse);
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateOrUpdateTerm failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("mark-known")]
    [ProducesResponseType(typeof(TermDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TermDetailsDto>> MarkKnown([FromBody] TermActionDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var grpcRequest = TermGrpcMapper.ToTermActionRequest(request, userId);
            var grpcResponse = await _vocabulary.MarkTermKnownAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(TermGrpcMapper.ToTermDetailsDto(grpcResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkTermKnown failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("ignore")]
    [ProducesResponseType(typeof(TermDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TermDetailsDto>> Ignore([FromBody] TermActionDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var grpcRequest = TermGrpcMapper.ToTermActionRequest(request, userId);
            var grpcResponse = await _vocabulary.IgnoreTermAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(TermGrpcMapper.ToTermDetailsDto(grpcResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IgnoreTerm failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("bulk-known")]
    [ProducesResponseType(typeof(BulkMarkKnownResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkMarkKnownResponseDto>> BulkMarkKnown([FromBody] BulkMarkKnownDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var grpcRequest = TermGrpcMapper.ToBulkMarkKnownRequest(request, userId);
            var grpcResponse = await _vocabulary.BulkMarkKnownAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(new BulkMarkKnownResponseDto { UpdatedCount = grpcResponse.UpdatedCount });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BulkMarkKnown failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpGet("details")]
    [ProducesResponseType(typeof(TermDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TermDetailsDto>> Details(
        [FromQuery] string projectId,
        [FromQuery] string termText,
        [FromQuery] string type = "WORD")
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(termText))
        {
            return BadRequest(new { error = "projectId and termText are required" });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var grpcRequest = TermGrpcMapper.ToGetTermDetailsRequest(projectId.Trim(), termText.Trim(), type.Trim(), userId);
            var grpcResponse = await _vocabulary.GetTermDetailsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(TermGrpcMapper.ToTermDetailsDto(grpcResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTermDetails failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("search-duplicates")]
    [ProducesResponseType(typeof(SearchTermDuplicatesResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchTermDuplicatesResponseDto>> SearchDuplicates([FromBody] SearchTermDuplicatesDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var grpcRequest = TermGrpcMapper.ToSearchDuplicatesRequest(request, userId);
            var grpcResponse = await _vocabulary.SearchTermDuplicatesAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(TermGrpcMapper.ToSearchDuplicatesDto(grpcResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchTermDuplicates failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>Удаляет демо-карточки импорта (Automation IMPORT) и связанные term rows.</summary>
    [HttpPost("purge-demo-import")]
    [ProducesResponseType(typeof(PurgeDemoImportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PurgeDemoImportResponseDto>> PurgeDemoImport([FromQuery] string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return BadRequest(new { error = "projectId is required" });

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);
            var grpcResponse = await _vocabulary.PurgeDemoImportAsync(
                new PurgeDemoImportRequest { ProjectId = projectId.Trim() },
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(new PurgeDemoImportResponseDto
            {
                CardsDeleted = grpcResponse.CardsDeleted,
                StatusesDeleted = grpcResponse.StatusesDeleted,
                TermsDeleted = grpcResponse.TermsDeleted,
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PurgeDemoImport failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    private ActionResult MapRpc(RpcException ex)
    {
        _logger.LogError(ex, "gRPC error in TermsController");
        var statusCode = ex.StatusCode switch
        {
            global::Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
            global::Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
            global::Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status502BadGateway
        };
        return StatusCode(statusCode, new { error = ex.Status.Detail });
    }
}
