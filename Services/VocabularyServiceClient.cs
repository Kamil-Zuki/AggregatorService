using AggregatorService.Dtos.Subscriptions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Pvs.Content.Grpc;
using static Pvs.Content.Grpc.ContentService;
using static Pvs.Content.Grpc.CardService;
using static Pvs.Content.Grpc.AnalyticsService;
using static Pvs.Content.Grpc.StudyService;
using static Pvs.Content.Grpc.CommunityService;
using static Pvs.Content.Grpc.SubscriptionService;
using static Pvs.Content.Grpc.TextService;
using static Pvs.Content.Grpc.TermService;
using AggregatorService.Options;

namespace AggregatorService.Services;

/// <summary>
/// Клиент для работы с VocabularyService через gRPC
/// </summary>
public class VocabularyServiceClient : IVocabularyServiceClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly ContentServiceClient _contentClient;
    private readonly CardServiceClient _cardClient;
    private readonly AnalyticsServiceClient _analyticsClient;
    private readonly StudyServiceClient _studyClient;
    private readonly CommunityServiceClient _communityClient;
    private readonly SubscriptionServiceClient _subscriptionClient;
    private readonly TextServiceClient _textClient;
    private readonly TermServiceClient _termClient;
    private readonly ILogger<VocabularyServiceClient> _logger;
    private readonly AggregatorServiceOptions _options;

    public VocabularyServiceClient(
        IOptions<AggregatorServiceOptions> options,
        ILogger<VocabularyServiceClient> logger,
        ContentServiceClient contentClient,
        CardServiceClient cardClient,
        AnalyticsServiceClient analyticsClient,
        StudyServiceClient studyClient,
        CommunityServiceClient communityClient,
        SubscriptionServiceClient subscriptionClient,
        TextServiceClient textClient,
        TermServiceClient termClient)
    {
        _options = options.Value;
        _logger = logger;
        _contentClient = contentClient;
        _cardClient = cardClient;
        _analyticsClient = analyticsClient;
        _studyClient = studyClient;
        _communityClient = communityClient;
        _subscriptionClient = subscriptionClient;
        _textClient = textClient;
        _termClient = termClient;
    }

    /// <summary>
    /// Создает новый проект в VocabularyService
    /// </summary>
    public async Task<ProjectResponse> CreateProjectAsync(
        CreateProjectRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Создаем метаданные для передачи user_id и roles в VocabularyService
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            // Устанавливаем user_id в запросе (из контекста HTTP запроса)
            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CreateProject request to VocabularyService for user {UserId}",
                userId);

            // Выполняем gRPC вызов с метаданными
            var response = await _contentClient.CreateProjectAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Project {ProjectId} created successfully for user {UserId}",
                response.Id,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating project for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating project for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Получает список проектов пользователя из VocabularyService
    /// </summary>
    public async Task<GetProjectsResponse> GetProjectsAsync(
        GetProjectsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Создаем метаданные для передачи user_id и roles в VocabularyService
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            // Устанавливаем user_id в запросе (из контекста HTTP запроса)
            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetProjects request to VocabularyService for user {UserId}, includeArchived: {IncludeArchived}",
                userId,
                request.IncludeArchived);

            // Выполняем gRPC вызов с метаданными
            var response = await _contentClient.GetProjectsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} projects for user {UserId}",
                response.Projects.Count,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting projects for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting projects for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Получает детали проекта из VocabularyService
    /// </summary>
    public async Task<ProjectResponse> GetProjectDetailsAsync(
        GetProjectDetailsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetProjectDetails request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            var response = await _contentClient.GetProjectDetailsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Project {ProjectId} retrieved successfully for user {UserId}",
                response.Id,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting project details for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting project details for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Обновляет проект в VocabularyService
    /// </summary>
    public async Task<ProjectResponse> UpdateProjectAsync(
        UpdateProjectRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending UpdateProject request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            var response = await _contentClient.UpdateProjectAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Project {ProjectId} updated successfully for user {UserId}",
                response.Id,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating project for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating project for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Удаляет проект в VocabularyService
    /// </summary>
    public async Task DeleteProjectAsync(
        DeleteProjectRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending DeleteProject request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            await _contentClient.DeleteProjectAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Project {ProjectId} deleted successfully in VocabularyService for user {UserId}",
                request.ProjectId,
                userId);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when deleting project for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when deleting project for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Получает статистику использованных лимитов пользователя из VocabularyService
    /// </summary>
    public async Task<GetUserUsageStatsResponse> GetUserUsageStatsAsync(
        GetUserUsageStatsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetUserUsageStats request to VocabularyService for user {UserId}",
                userId);

            var response = await _contentClient.GetUserUsageStatsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting user usage stats for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting user usage stats for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Получает дерево колод для проекта из VocabularyService
    /// </summary>

    public async Task<GetDeckTreeResponse> GetDeckTreeAsync(
        GetDeckTreeRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetDeckTree request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            var response = await _contentClient.GetDeckTreeAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Deck tree retrieved successfully for project {ProjectId}",
                request.ProjectId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting deck tree for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting deck tree for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Получает детальную информацию о колоде из VocabularyService
    /// </summary>
    public async Task<GetDeckDetailResponse> GetDeckDetailAsync(
        GetDeckDetailRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var metadata = new Metadata
        {
            { "user_id", userId.ToString() },
            { "roles", string.Join(",", roles) }
        };

        request.UserId = userId.ToString();

        _logger.LogInformation(
            "Sending GetDeckDetail request to VocabularyService for user {UserId}, deck {DeckId}",
            userId,
            request.DeckId);

        return await _contentClient.GetDeckDetailAsync(
            request,
            headers: metadata,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Создает новую колоду в VocabularyService
    /// </summary>
    public async Task<DeckResponse> CreateDeckAsync(
        CreateDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CreateDeck request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            var response = await _contentClient.CreateDeckAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Deck {DeckId} created successfully for user {UserId}",
                response.Id,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating deck for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating deck for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Обновляет колоду в VocabularyService
    /// </summary>
    public async Task<DeckResponse> UpdateDeckAsync(
        UpdateDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending UpdateDeck request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _contentClient.UpdateDeckAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Deck {DeckId} updated successfully for user {UserId}",
                response.Id,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating deck for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating deck for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Удаляет колоду в VocabularyService
    /// </summary>
    public async Task<Google.Protobuf.WellKnownTypes.Empty> DeleteDeckAsync(
        DeleteDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending DeleteDeck request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _contentClient.DeleteDeckAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Deck {DeckId} deleted successfully for user {UserId}",
                request.DeckId,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when deleting deck for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when deleting deck for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Получает настройки пользователя из VocabularyService
    /// </summary>
    public async Task<UserSettingsResponse> GetUserSettingsAsync(
        GetUserSettingsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetUserSettings request to VocabularyService for user {UserId}",
                userId);

            var response = await _contentClient.GetUserSettingsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "User settings retrieved successfully for user {UserId}",
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting user settings for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting user settings for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Обновляет настройки пользователя в VocabularyService
    /// </summary>
    public async Task<UserSettingsResponse> UpdateUserSettingsAsync(
        UpdateUserSettingsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending UpdateUserSettings request to VocabularyService for user {UserId}",
                userId);

            var response = await _contentClient.UpdateUserSettingsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "User settings updated successfully for user {UserId}",
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating user settings for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating user settings for user {UserId}", userId);
            throw;
        }
    }

    // ========== CardService Methods ==========

    public async Task<CardResponse> CreateCardAsync(
        CreateCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CreateCard request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _cardClient.CreateCardAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Card {CardId} created successfully for user {UserId}",
                response.Id,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating card for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating card for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CheckCardDuplicatesResponse> CheckCardDuplicatesAsync(
        CheckCardDuplicatesRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CheckCardDuplicates request to VocabularyService for user {UserId}, project {ProjectId}, term {TermText}",
                userId,
                request.ProjectId,
                request.TermText);

            return await _cardClient.CheckCardDuplicatesAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when checking duplicate cards for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when checking duplicate cards for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CardResponse> CaptureCardAsync(
        CaptureCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CaptureCard request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            var response = await _cardClient.CaptureCardAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Card {CardId} captured successfully for user {UserId}",
                response.Id,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when capturing card for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when capturing card for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CardResponse> GetCardAsync(
        GetCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetCard request to VocabularyService for user {UserId}, card {CardId}",
                userId,
                request.CardId);

            var response = await _cardClient.GetCardAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Card {CardId} retrieved successfully for user {UserId}",
                request.CardId,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting card for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting card for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetNoteTypeForEditorResponse> GetNoteTypeForEditorAsync(
        GetNoteTypeForEditorRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            return await _cardClient.GetNoteTypeForEditorAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error GetNoteTypeForEditor for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error GetNoteTypeForEditor for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CardResponse> UpdateCardAsync(
        UpdateCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending UpdateCard request to VocabularyService for user {UserId}, card {CardId}",
                userId,
                request.CardId);

            var response = await _cardClient.UpdateCardAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Card {CardId} updated successfully for user {UserId}",
                request.CardId,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating card for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating card for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Google.Protobuf.WellKnownTypes.Empty> DeleteCardAsync(
        DeleteCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending DeleteCard request to VocabularyService for user {UserId}, card {CardId}",
                userId,
                request.CardId);

            var response = await _cardClient.DeleteCardAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Card {CardId} deleted successfully for user {UserId}",
                request.CardId,
                userId);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when deleting card for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when deleting card for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SearchCardsResponse> SearchCardsAsync(
        SearchCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending SearchCards request to VocabularyService for user {UserId}, query: {Query}",
                userId,
                request.Query);

            var response = await _cardClient.SearchCardsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "SearchCards completed for user {UserId}, found {Count} cards",
                userId,
                response.TotalCount);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when searching cards for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when searching cards for user {UserId}", userId);
            throw;
        }
    }

    public async Task<BulkCreateCardsResponse> BulkCreateCardsAsync(
        BulkCreateCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending BulkCreateCards request to VocabularyService for user {UserId}, deck {DeckId}, count: {Count}",
                userId,
                request.DeckId,
                request.Cards.Count);

            var response = await _cardClient.BulkCreateCardsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "BulkCreateCards completed for user {UserId}, created {Count} cards",
                userId,
                response.CreatedCards.Count);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when bulk creating cards for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when bulk creating cards for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Google.Protobuf.WellKnownTypes.Empty> BulkDeleteCardsAsync(
        BulkDeleteCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            return await _cardClient.BulkDeleteCardsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when bulk deleting cards for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when bulk deleting cards for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Google.Protobuf.WellKnownTypes.Empty> MoveCardsAsync(
        MoveCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            return await _cardClient.MoveCardsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when moving cards for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when moving cards for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Google.Protobuf.WellKnownTypes.Empty> ResetCardProgressAsync(
        ResetCardProgressRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            return await _cardClient.ResetCardProgressAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when resetting card progress for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when resetting card progress for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetLeechCardsResponse> GetLeechCardsAsync(
        GetLeechCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            return await _cardClient.GetLeechCardsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting leech cards for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting leech cards for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetCardsMissingMediaResponse> GetCardsMissingMediaAsync(
        GetCardsMissingMediaRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            return await _cardClient.GetCardsMissingMediaAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting cards missing media for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting cards missing media for user {UserId}", userId);
            throw;
        }
    }

    public async Task<AnalyzeTextResponse> AnalyzeTextAsync(
        AnalyzeTextRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending AnalyzeText request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            return await _textClient.AnalyzeTextAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when analyzing text for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when analyzing text for user {UserId}", userId);
            throw;
        }
    }

    public Task<TermDetailsResponse> CreateOrUpdateTermAsync(
        CreateOrUpdateTermRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _termClient.CreateOrUpdateTermAsync(request, headers: BuildMetadata(userId, roles), cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<TermDetailsResponse> MarkTermKnownAsync(
        TermActionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _termClient.MarkTermKnownAsync(request, headers: BuildMetadata(userId, roles), cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<TermDetailsResponse> IgnoreTermAsync(
        TermActionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _termClient.IgnoreTermAsync(request, headers: BuildMetadata(userId, roles), cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<BulkMarkKnownResponse> BulkMarkKnownAsync(
        BulkMarkKnownRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _termClient.BulkMarkKnownAsync(request, headers: BuildMetadata(userId, roles), cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<TermDetailsResponse> GetTermDetailsAsync(
        GetTermDetailsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _termClient.GetTermDetailsAsync(request, headers: BuildMetadata(userId, roles), cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<SearchTermDuplicatesResponse> SearchTermDuplicatesAsync(
        SearchTermDuplicatesRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _termClient.SearchTermDuplicatesAsync(request, headers: BuildMetadata(userId, roles), cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<ListProjectTermsResponse> ListProjectTermsAsync(
        ListProjectTermsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _termClient.ListProjectTermsAsync(request, headers: BuildMetadata(userId, roles), cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<PurgeDemoImportResponse> PurgeDemoImportAsync(
        PurgeDemoImportRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _termClient.PurgeDemoImportAsync(request, headers: BuildMetadata(userId, roles), cancellationToken: cancellationToken).ResponseAsync;
    }

    // ========== AnalyticsService Methods ==========

    public async Task<GetVocabularyStatsResponse> GetVocabularyStatsAsync(
        GetVocabularyStatsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetVocabularyStats request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            var response = await _analyticsClient.GetVocabularyStatsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting vocabulary stats for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting vocabulary stats for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetSkillBalanceResponse> GetSkillBalanceAsync(
        GetSkillBalanceRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetSkillBalance request to VocabularyService for user {UserId}, projectId: {ProjectId}",
                userId,
                request.ProjectId);

            var response = await _analyticsClient.GetSkillBalanceAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting skill balance for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting skill balance for user {UserId}", userId);
            throw;
        }
    }


    public async Task<GetHeatmapResponse> GetHeatmapAsync(
        GetHeatmapRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetHeatmap request to VocabularyService for user {UserId}, year {Year}",
                userId,
                request.Year);

            var response = await _analyticsClient.GetHeatmapAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting heatmap for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting heatmap for user {UserId}", userId);
            throw;
        }
    }

    private static Metadata BuildMetadata(Guid userId, IEnumerable<string> roles)
    {
        return new Metadata
        {
            { "user_id", userId.ToString() },
            { "roles", string.Join(",", roles) }
        };
    }

    public async Task<GetDailySummaryResponse> GetDailySummaryAsync(
        GetDailySummaryRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetDailySummary request to VocabularyService for user {UserId}",
                userId);

            var response = await _analyticsClient.GetDailySummaryAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting daily summary for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting daily summary for user {UserId}", userId);
            throw;
        }
    }

    // ========== StudyService Methods ==========

    public async Task<StartStudySessionResponse> StartStudySessionAsync(
        StartStudySessionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending StartStudySession request to VocabularyService for user {UserId}, project {ProjectId}",
                userId,
                request.ProjectId);

            var response = await _studyClient.StartStudySessionAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when starting study session for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when starting study session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetNextCardResponse> GetNextCardAsync(
        GetNextCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetNextCard request to VocabularyService for user {UserId}, session {SessionId}",
                userId,
                request.SessionId);

            var response = await _studyClient.GetNextCardAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting next card for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting next card for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SubmitReviewResponse> SubmitReviewAsync(
        SubmitReviewRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending SubmitReview request to VocabularyService for user {UserId}, session {SessionId}, card {CardId}",
                userId,
                request.SessionId,
                request.CardId);

            var response = await _studyClient.SubmitReviewAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when submitting review for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when submitting review for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UndoReviewResponse> UndoReviewAsync(
        UndoReviewRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending UndoReview request to VocabularyService for user {UserId}, session {SessionId}",
                userId,
                request.SessionId);

            var response = await _studyClient.UndoReviewAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when undoing review for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when undoing review for user {UserId}", userId);
            throw;
        }
    }

    // ========== CommunityService Methods ==========

    // Contributions

    public async Task<CreateContributionResponse> CreateContributionAsync(
        CreateContributionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CreateContribution request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _communityClient.CreateContributionAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating contribution for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating contribution for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetContributionsResponse> GetContributionsAsync(
        GetContributionsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetContributions request to VocabularyService for user {UserId}",
                userId);

            var response = await _communityClient.GetContributionsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting contributions for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting contributions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetContributionResponse> GetContributionAsync(
        GetContributionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetContribution request to VocabularyService for user {UserId}, contribution {ContributionId}",
                userId,
                request.ContributionId);

            var response = await _communityClient.GetContributionAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting contribution for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting contribution for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ResolveContributionResponse> ResolveContributionAsync(
        ResolveContributionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending ResolveContribution request to VocabularyService for user {UserId}, contribution {ContributionId}",
                userId,
                request.ContributionId);

            var response = await _communityClient.ResolveContributionAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when resolving contribution for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when resolving contribution for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UpdateContributionPolicyResponse> UpdateContributionPolicyAsync(
        UpdateContributionPolicyRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending UpdateContributionPolicy request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _communityClient.UpdateContributionPolicyAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating contribution policy for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating contribution policy for user {UserId}", userId);
            throw;
        }
    }

    // Publishing

    public async Task<PublishDeckResponse> PublishDeckAsync(
        PublishDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending PublishDeck request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _communityClient.PublishDeckAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when publishing deck for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when publishing deck for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ForkDeckResponse> ForkDeckAsync(
        ForkDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending ForkDeck request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _communityClient.ForkDeckAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when forking deck for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when forking deck for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetPublishedDecksResponse> GetPublishedDecksAsync(
        GetPublishedDecksRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetPublishedDecks request to VocabularyService for user {UserId}",
                userId);

            var response = await _communityClient.GetPublishedDecksAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting published decks for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting published decks for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetAuthorProfileResponse> GetAuthorProfileAsync(
        GetAuthorProfileRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetAuthorProfile request to VocabularyService for user {UserId}, author {AuthorId}",
                userId,
                request.AuthorId);

            var response = await _communityClient.GetAuthorProfileAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting author profile for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting author profile for user {UserId}", userId);
            throw;
        }
    }

    // Marketplace

    public async Task<CreateProductResponse> CreateProductAsync(
        CreateProductRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CreateProduct request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _communityClient.CreateProductAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating product for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating product for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UpdateProductResponse> UpdateProductAsync(
        UpdateProductRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending UpdateProduct request to VocabularyService for user {UserId}, product {ProductId}",
                userId,
                request.ProductId);

            var response = await _communityClient.UpdateProductAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating product for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating product for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetProductsResponse> GetProductsAsync(
        GetProductsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetProducts request to VocabularyService for user {UserId}",
                userId);

            var response = await _communityClient.GetProductsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting products for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting products for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetProductDetailsResponse> GetProductDetailsAsync(
        GetProductDetailsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetProductDetails request to VocabularyService for user {UserId}, product {ProductId}",
                userId,
                request.ProductId);

            var response = await _communityClient.GetProductDetailsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting product details for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting product details for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CreateReviewResponse> CreateReviewAsync(
        CreateReviewRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CreateReview request to VocabularyService for user {UserId}, product {ProductId}",
                userId,
                request.ProductId);

            var response = await _communityClient.CreateReviewAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when creating review for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating review for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetProductStatsResponse> GetProductStatsAsync(
        GetProductStatsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending GetProductStats request to VocabularyService for user {UserId}, product {ProductId}",
                userId,
                request.ProductId);

            var response = await _communityClient.GetProductStatsAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting product stats for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting product stats for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CheckEntitlementResponse> CheckEntitlementAsync(
        CheckEntitlementRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            request.UserId = userId.ToString();

            _logger.LogInformation(
                "Sending CheckEntitlement request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            var response = await _communityClient.CheckEntitlementAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);

            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when checking entitlement for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when checking entitlement for user {UserId}", userId);
            throw;
        }
    }

    // ========== Subscriptions (stub – not yet implemented via gRPC) ==========

    /// <inheritdoc />
    public Task<IReadOnlyList<SubscriptionListItemDto>> ListSubscriptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() }
            };

            _logger.LogInformation(
                "Sending ListSubscriptions request to VocabularyService for user {UserId}",
                userId);

            return ListSubscriptionsInternalAsync(metadata, cancellationToken);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when listing subscriptions for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<SubscriptionListItemDto> SubscribeAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() }
            };

            var request = new SubscribeRequest
            {
                DeckId = deckId.ToString()
            };

            _logger.LogInformation(
                "Sending Subscribe request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                deckId);

            return SubscribeInternalAsync(request, metadata, cancellationToken);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when subscribing to deck {DeckId} for user {UserId}", deckId, userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() }
            };

            var request = new UnsubscribeRequest
            {
                DeckId = deckId.ToString()
            };

            _logger.LogInformation(
                "Sending Unsubscribe request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                deckId);

            await _subscriptionClient.UnsubscribeAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when unsubscribing from deck {DeckId} for user {UserId}", deckId, userId);
            throw;
        }
    }

    private async Task<IReadOnlyList<SubscriptionListItemDto>> ListSubscriptionsInternalAsync(
        Metadata metadata,
        CancellationToken cancellationToken)
    {
        var response = await _subscriptionClient.ListSubscriptionsAsync(
            new ListSubscriptionsRequest(),
            headers: metadata,
            cancellationToken: cancellationToken);

        return response.Items
            .Select(MapSubscriptionItem)
            .ToList();
    }

    private async Task<SubscriptionListItemDto> SubscribeInternalAsync(
        SubscribeRequest request,
        Metadata metadata,
        CancellationToken cancellationToken)
    {
        var response = await _subscriptionClient.SubscribeAsync(
            request,
            headers: metadata,
            cancellationToken: cancellationToken);

        return MapSubscriptionItem(response);
    }

    private static SubscriptionListItemDto MapSubscriptionItem(SubscriptionItemResponse item)
    {
        return new SubscriptionListItemDto
        {
            DeckId = Guid.Parse(item.DeckId),
            ProjectId = Guid.Parse(item.ProjectId),
            Title = item.Title,
            SubscribedAt = item.SubscribedAt.ToDateTime(),
            LastAccessedAt = item.LastAccessedAt.ToDateTime(),
            LastSyncedVersion = item.LastSyncedVersion
        };
    }

    public async Task<GetDailyAutopilotPlanResponse> GetDailyAutopilotPlanAsync(string projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var request = new GetDailyAutopilotPlanRequest { ProjectId = projectId };
        var headers = new Metadata
        {
            { "user_id", userId.ToString() }
        };
        return await _analyticsClient.GetDailyAutopilotPlanAsync(request, headers, cancellationToken: cancellationToken);
    }

    public async Task<TrackSkillActivityResponse> TrackSkillActivityAsync(
        string projectId,
        Guid userId,
        int skillTypeId,
        int value,
        CancellationToken cancellationToken = default)
    {
        var request = new TrackSkillActivityRequest
        {
            ProjectId = projectId,
            SkillTypeId = skillTypeId,
            Value = value
        };
        var headers = new Metadata
        {
            { "user_id", userId.ToString() }
        };
        return await _analyticsClient.TrackSkillActivityAsync(request, headers, cancellationToken: cancellationToken);
    }

    public async Task<StartImportJobResponse> StartImportJobAsync(
        StartImportJobRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            _logger.LogInformation(
                "Sending StartImportJob request to VocabularyService for user {UserId}, deck {DeckId}",
                userId,
                request.DeckId);

            return await _cardClient.StartImportJobAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when starting import job for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when starting import job for user {UserId}", userId);
            throw;
        }
    }

    public async Task<GetImportJobStatusResponse> GetImportJobStatusAsync(
        GetImportJobStatusRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            _logger.LogInformation(
                "Sending GetImportJobStatus request to VocabularyService for user {UserId}, job {JobId}",
                userId,
                request.JobId);

            return await _cardClient.GetImportJobStatusAsync(
                request,
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            if (ex.StatusCode != Grpc.Core.StatusCode.NotFound)
            {
                _logger.LogError(ex, "gRPC error when getting import job status for user {UserId}", userId);
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting import job status for user {UserId}", userId);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
