using AggregatorService.Dtos;
using AggregatorService.Dtos.Subscriptions;
using AggregatorService.Helpers;
using AggregatorService.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.Controllers;

/// <summary>
/// Controller for current user's deck subscriptions (list, subscribe, unsubscribe).
/// </summary>
[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyServiceClient;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        IVocabularyServiceClient vocabularyServiceClient,
        ILogger<SubscriptionsController> logger)
    {
        _vocabularyServiceClient = vocabularyServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Lists the current user's deck subscriptions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeckSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<DeckSubscriptionDto>>> ListSubscriptions(CancellationToken cancellationToken)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);

            _logger.LogInformation("ListSubscriptions request from user {UserId}", userId);

            var items = await _vocabularyServiceClient.ListSubscriptionsAsync(
                userId,
                cancellationToken);

            var result = items
                .Select(i => MapToDeckSubscriptionDto(i, userId))
                .ToList();

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when listing subscriptions");

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
            _logger.LogError(ex, "Error listing subscriptions");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Subscribes the current user to a deck.
    /// </summary>
    [HttpPost("{deckId}")]
    [ProducesResponseType(typeof(DeckSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeckSubscriptionDto>> Subscribe(Guid deckId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);

            _logger.LogInformation("Subscribe request from user {UserId} for deck {DeckId}", userId, deckId);

            var subscription = await _vocabularyServiceClient.SubscribeAsync(
                userId,
                deckId,
                cancellationToken);

            var dto = MapToDeckSubscriptionDto(subscription, userId);

            return CreatedAtAction(
                nameof(Subscribe),
                new { deckId },
                dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when subscribing to deck");

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
            _logger.LogError(ex, "Error subscribing to deck");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Unsubscribes the current user from a deck (idempotent).
    /// </summary>
    [HttpDelete("{deckId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Unsubscribe(Guid deckId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);

            _logger.LogInformation("Unsubscribe request from user {UserId} for deck {DeckId}", userId, deckId);

            await _vocabularyServiceClient.UnsubscribeAsync(
                userId,
                deckId,
                cancellationToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when unsubscribing from deck");

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
            _logger.LogError(ex, "Error unsubscribing from deck");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    private static DeckSubscriptionDto MapToDeckSubscriptionDto(SubscriptionListItemDto item, Guid userId)
    {
        return new DeckSubscriptionDto
        {
            Id = item.DeckId.ToString(),
            UserId = userId.ToString(),
            DeckId = item.DeckId.ToString(),
            LastSyncedVersion = item.LastSyncedVersion,
            SubscribedAt = item.SubscribedAt,
            LastAccessedAt = item.LastAccessedAt ?? item.SubscribedAt,
            DeckTitle = item.Title
        };
    }
}
