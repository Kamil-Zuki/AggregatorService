using AggregatorService.Dtos.Subscriptions;
using Pvs.Content.Grpc;

namespace AggregatorService.Services;

/// <summary>
/// Интерфейс клиента для работы с VocabularyService через gRPC
/// </summary>
public interface IVocabularyServiceClient
{
    /// <summary>
    /// Создает новый проект в VocabularyService
    /// </summary>
    /// <param name="request">Запрос на создание проекта</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с данными созданного проекта</returns>
    Task<ProjectResponse> CreateProjectAsync(
        CreateProjectRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список проектов пользователя из VocabularyService
    /// </summary>
    /// <param name="request">Запрос на получение проектов</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ со списком проектов</returns>
    Task<GetProjectsResponse> GetProjectsAsync(
        GetProjectsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает детали проекта из VocabularyService
    /// </summary>
    /// <param name="request">Запрос на получение деталей проекта</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с данными проекта</returns>
    Task<ProjectResponse> GetProjectDetailsAsync(
        GetProjectDetailsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет проект в VocabularyService
    /// </summary>
    /// <param name="request">Запрос на обновление проекта</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с обновленными данными проекта</returns>
    Task<ProjectResponse> UpdateProjectAsync(
        UpdateProjectRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет проект в VocabularyService
    /// </summary>
    Task DeleteProjectAsync(
        DeleteProjectRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает статистику использованных лимитов пользователя
    /// </summary>
    Task<GetUserUsageStatsResponse> GetUserUsageStatsAsync(
        GetUserUsageStatsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает дерево колод для проекта из VocabularyService
    /// </summary>
    /// <param name="request">Запрос на получение дерева колод</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с деревом колод</returns>
    Task<GetDeckTreeResponse> GetDeckTreeAsync(
        GetDeckTreeRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает детальную информацию о колоде из VocabularyService
    /// </summary>
    /// <param name="request">Запрос на получение деталей колоды</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с деталями колоды и статистикой карточек</returns>
    Task<GetDeckDetailResponse> GetDeckDetailAsync(
        GetDeckDetailRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает новую колоду в VocabularyService
    /// </summary>
    /// <param name="request">Запрос на создание колоды</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с данными созданной колоды</returns>
    Task<DeckResponse> CreateDeckAsync(
        CreateDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет колоду в VocabularyService
    /// </summary>
    /// <param name="request">Запрос на обновление колоды</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с обновленными данными колоды</returns>
    Task<DeckResponse> UpdateDeckAsync(
        UpdateDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет колоду в VocabularyService
    /// </summary>
    /// <param name="request">Запрос на удаление колоды</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Пустой ответ</returns>
    Task<Google.Protobuf.WellKnownTypes.Empty> DeleteDeckAsync(
        DeleteDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает настройки пользователя из VocabularyService
    /// </summary>
    /// <param name="request">Запрос на получение настроек</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с настройками пользователя</returns>
    Task<UserSettingsResponse> GetUserSettingsAsync(
        GetUserSettingsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет настройки пользователя в VocabularyService
    /// </summary>
    /// <param name="request">Запрос на обновление настроек</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="roles">Список ролей пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ с обновленными настройками пользователя</returns>
    Task<UserSettingsResponse> UpdateUserSettingsAsync(
        UpdateUserSettingsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    // ========== CardService Methods ==========

    /// <summary>
    /// Создает карточку в VocabularyService
    /// </summary>
    Task<CardResponse> CreateCardAsync(
        CreateCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, есть ли в проекте карточки с той же леммой.
    /// </summary>
    Task<CheckCardDuplicatesResponse> CheckCardDuplicatesAsync(
        CheckCardDuplicatesRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Захватывает карточку из внешнего источника
    /// </summary>
    Task<CardResponse> CaptureCardAsync(
        CaptureCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает карточку по идентификатору
    /// </summary>
    Task<CardResponse> GetCardAsync(
        GetCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetNoteTypeForEditorResponse> GetNoteTypeForEditorAsync(
        GetNoteTypeForEditorRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет карточку в VocabularyService
    /// </summary>
    Task<CardResponse> UpdateCardAsync(
        UpdateCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет карточку в VocabularyService
    /// </summary>
    Task<Google.Protobuf.WellKnownTypes.Empty> DeleteCardAsync(
        DeleteCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет полнотекстовый поиск карточек
    /// </summary>
    Task<SearchCardsResponse> SearchCardsAsync(
        SearchCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Массовое создание карточек
    /// </summary>
    Task<BulkCreateCardsResponse> BulkCreateCardsAsync(
        BulkCreateCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<StartImportJobResponse> StartImportJobAsync(
        StartImportJobRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetImportJobStatusResponse> GetImportJobStatusAsync(
        GetImportJobStatusRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<Google.Protobuf.WellKnownTypes.Empty> BulkDeleteCardsAsync(
        BulkDeleteCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<Google.Protobuf.WellKnownTypes.Empty> MoveCardsAsync(
        MoveCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<Google.Protobuf.WellKnownTypes.Empty> ResetCardProgressAsync(
        ResetCardProgressRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetLeechCardsResponse> GetLeechCardsAsync(
        GetLeechCardsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetCardsMissingMediaResponse> GetCardsMissingMediaAsync(
        GetCardsMissingMediaRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<AnalyzeTextResponse> AnalyzeTextAsync(
        AnalyzeTextRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<TermDetailsResponse> CreateOrUpdateTermAsync(
        CreateOrUpdateTermRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<TermDetailsResponse> MarkTermKnownAsync(
        TermActionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<TermDetailsResponse> IgnoreTermAsync(
        TermActionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<BulkMarkKnownResponse> BulkMarkKnownAsync(
        BulkMarkKnownRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<TermDetailsResponse> GetTermDetailsAsync(
        GetTermDetailsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<SearchTermDuplicatesResponse> SearchTermDuplicatesAsync(
        SearchTermDuplicatesRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<ListProjectTermsResponse> ListProjectTermsAsync(
        ListProjectTermsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<PurgeDemoImportResponse> PurgeDemoImportAsync(
        PurgeDemoImportRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    // ========== AnalyticsService Methods ==========

    /// <summary>
    /// Получает статистику словарного запаса (SR-ANL-01)
    /// </summary>
    Task<GetVocabularyStatsResponse> GetVocabularyStatsAsync(
        GetVocabularyStatsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает данные для календаря активности (SR-ANL-02)
    /// </summary>
    Task<GetHeatmapResponse> GetHeatmapAsync(
        GetHeatmapRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает дневную сводку и информацию о серии (SR-ANL-03)
    /// </summary>
    Task<GetDailySummaryResponse> GetDailySummaryAsync(
        GetDailySummaryRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetSkillBalanceResponse> GetSkillBalanceAsync(
        GetSkillBalanceRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    // ========== StudyService Methods ==========

    /// <summary>
    /// Старт новой сессии обучения (SR-LRN-01)
    /// </summary>
    Task<StartStudySessionResponse> StartStudySessionAsync(
        StartStudySessionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение следующей карточки (SR-LRN-02)
    /// </summary>
    Task<GetNextCardResponse> GetNextCardAsync(
        GetNextCardRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправка оценки (FSRS) (SR-LRN-03)
    /// </summary>
    Task<SubmitReviewResponse> SubmitReviewAsync(
        SubmitReviewRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отмена последнего действия (SR-LRN-08)
    /// </summary>
    Task<UndoReviewResponse> UndoReviewAsync(
        UndoReviewRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    // ========== CommunityService Methods ==========

    // Contributions (SR-COL-01 до SR-COL-08)
    
    /// <summary>
    /// Создает предложение (SR-COL-01)
    /// </summary>
    Task<CreateContributionResponse> CreateContributionAsync(
        CreateContributionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список предложений (SR-COL-03)
    /// </summary>
    Task<GetContributionsResponse> GetContributionsAsync(
        GetContributionsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает предложение с Diff (SR-COL-03)
    /// </summary>
    Task<GetContributionResponse> GetContributionAsync(
        GetContributionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Принимает/отклоняет предложение (SR-COL-04)
    /// </summary>
    Task<ResolveContributionResponse> ResolveContributionAsync(
        ResolveContributionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет политику вкладов (SR-COL-06)
    /// </summary>
    Task<UpdateContributionPolicyResponse> UpdateContributionPolicyAsync(
        UpdateContributionPolicyRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    // Publishing (SR-PUB-01 до SR-PUB-04)

    /// <summary>
    /// Публикует колоду (SR-PUB-01)
    /// </summary>
    Task<PublishDeckResponse> PublishDeckAsync(
        PublishDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Клонирует колоду (SR-PUB-02)
    /// </summary>
    Task<ForkDeckResponse> ForkDeckAsync(
        ForkDeckRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает опубликованные колоды (SR-PUB-01)
    /// </summary>
    Task<GetPublishedDecksResponse> GetPublishedDecksAsync(
        GetPublishedDecksRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает профиль автора (SR-PUB-04)
    /// </summary>
    Task<GetAuthorProfileResponse> GetAuthorProfileAsync(
        GetAuthorProfileRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    // Marketplace (SR-MKT-01 до SR-MKT-06)

    /// <summary>
    /// Создает товар (SR-MKT-01)
    /// </summary>
    Task<CreateProductResponse> CreateProductAsync(
        CreateProductRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет товар (SR-MKT-01)
    /// </summary>
    Task<UpdateProductResponse> UpdateProductAsync(
        UpdateProductRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список товаров (SR-MKT-01)
    /// </summary>
    Task<GetProductsResponse> GetProductsAsync(
        GetProductsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает детали товара (SR-MKT-02)
    /// </summary>
    Task<GetProductDetailsResponse> GetProductDetailsAsync(
        GetProductDetailsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает отзыв (SR-MKT-05)
    /// </summary>
    Task<CreateReviewResponse> CreateReviewAsync(
        CreateReviewRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает статистику товара (SR-MKT-06)
    /// </summary>
    Task<GetProductStatsResponse> GetProductStatsAsync(
        GetProductStatsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет права доступа (SR-MKT-03, SR-COL-07)
    /// </summary>
    Task<CheckEntitlementResponse> CheckEntitlementAsync(
        CheckEntitlementRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    // ========== Subscriptions (deck subscriptions) ==========

    /// <summary>
    /// Lists current user's deck subscriptions.
    /// </summary>
    Task<IReadOnlyList<SubscriptionListItemDto>> ListSubscriptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes the current user to a deck.
    /// </summary>
    Task<SubscriptionListItemDto> SubscribeAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes the current user from a deck (idempotent).
    /// </summary>
    Task UnsubscribeAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default);

    Task<GetDailyAutopilotPlanResponse> GetDailyAutopilotPlanAsync(string projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks user skill activity (e.g. reading minutes, writing exercises) for today.
    /// Uses upsert: value is accumulated, not replaced.
    /// </summary>
    Task<TrackSkillActivityResponse> TrackSkillActivityAsync(
        string projectId,
        Guid userId,
        int skillTypeId,
        int value,
        CancellationToken cancellationToken = default);
}
