using AggregatorService.Dtos.Reader;
using AggregatorService.Helpers;
using AggregatorService.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pvs.Content.Grpc;

namespace AggregatorService.Controllers;

/// <summary>
/// REST-мост к TextService (анализ текста для Reader).
/// </summary>
[ApiController]
[Route("api/text")]
[Authorize]
public class TextController : ControllerBase
{
    private const int MaxAnalyzeTextLength = 100_000;

    private readonly IVocabularyServiceClient _vocabulary;
    private readonly ILogger<TextController> _logger;

    public TextController(IVocabularyServiceClient vocabulary, ILogger<TextController> logger)
    {
        _vocabulary = vocabulary;
        _logger = logger;
    }

    /// <summary>
    /// Токенизация и статусы терминов для подсветки в Reader.
    /// </summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(TextAnalyzeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TextAnalyzeResponseDto>> Analyze([FromBody] TextAnalyzeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectId) || request.Text == null)
        {
            return BadRequest(new { error = "projectId and text are required" });
        }

        if (request.Text.Length > MaxAnalyzeTextLength)
        {
            return BadRequest(new
            {
                error = "InvalidRequest",
                message = $"Text is too long (max {MaxAnalyzeTextLength} characters)"
            });
        }

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new AnalyzeTextRequest
            {
                ProjectId = request.ProjectId.Trim(),
                Text = request.Text
            };

            var grpcResponse = await _vocabulary.AnalyzeTextAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(ReaderTextMapper.ToHttpResponse(grpcResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized text analyze");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error in AnalyzeText");
            var statusCode = ex.StatusCode switch
            {
                global::Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                global::Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                global::Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status502BadGateway
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalyzeText");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
