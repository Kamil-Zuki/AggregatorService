using Pvs.Media.Grpc;

namespace AggregatorService.Services;

/// <summary>
/// Клиент Aggregator → MediaService (gRPC). Один сервис — одна точка вызова.
/// </summary>
public interface IMediaServiceClient
{
    Task<UploadImageResponse> UploadImageAsync(
        UploadImageRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<UploadAudioResponse> UploadAudioAsync(
        UploadAudioRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetImageUrlResponse> GetImageUrlAsync(
        string imageId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<UploadDocumentResponse> UploadDocumentAsync(
        UploadDocumentRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<ListReaderLibraryBooksResponse> ListReaderLibraryBooksAsync(
        string projectId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<SaveReaderLibraryBookResponse> SaveReaderLibraryBookAsync(
        SaveReaderLibraryBookRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task DeleteReaderLibraryBookAsync(
        string projectId,
        string bookId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<ListReaderCollectionsResponse> ListReaderCollectionsAsync(
        string projectId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<SaveReaderCollectionResponse> SaveReaderCollectionAsync(
        SaveReaderCollectionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task DeleteReaderCollectionAsync(
        string projectId,
        string collectionId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<ShareReaderCollectionResponse> ShareReaderCollectionAsync(
        ShareReaderCollectionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<UnshareReaderCollectionResponse> UnshareReaderCollectionAsync(
        UnshareReaderCollectionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<ListSharedReaderCollectionsResponse> ListSharedReaderCollectionsAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Внутренний URL для скачивания документа (Aggregator проксирует через HTTP).
    /// </summary>
    Task<GetDocumentUrlResponse> GetDocumentUrlAsync(
        GetDocumentUrlRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task PutDocumentExtractAsync(
        PutDocumentExtractRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetDocumentExtractResponse> GetDocumentExtractAsync(
        GetDocumentExtractRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentExtractAsync(
        DeleteDocumentExtractRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task DeleteProjectMediaAsync(
        DeleteProjectMediaRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}
