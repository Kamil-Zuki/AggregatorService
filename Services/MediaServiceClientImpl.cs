using Grpc.Core;
using Pvs.Media.Grpc;
using static Pvs.Media.Grpc.MediaService;

namespace AggregatorService.Services;

/// <summary>
/// Реализация gRPC-клиента MediaService с передачей user_id/roles в метаданных (как в VocabularyService).
/// </summary>
public class MediaServiceClientImpl : IMediaServiceClient
{
    private readonly MediaServiceClient _grpc;
    private readonly ILogger<MediaServiceClientImpl> _logger;

    public MediaServiceClientImpl(MediaServiceClient grpc, ILogger<MediaServiceClientImpl> logger)
    {
        _grpc = grpc;
        _logger = logger;
    }

    private static Metadata BuildMetadata(Guid userId, IEnumerable<string> roles)
    {
        return new Metadata
        {
            { "user_id", userId.ToString() },
            { "roles", string.Join(",", roles) }
        };
    }

    public async Task<UploadImageResponse> UploadImageAsync(
        UploadImageRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UploadImage for user {UserId}", userId);
        return await _grpc.UploadImageAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<UploadAudioResponse> UploadAudioAsync(
        UploadAudioRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UploadAudio for user {UserId}", userId);
        return await _grpc.UploadAudioAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<GetImageUrlResponse> GetImageUrlAsync(
        string imageId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var request = new GetImageUrlRequest { ImageId = imageId };
        return await _grpc.GetImageUrlAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<UploadDocumentResponse> UploadDocumentAsync(
        UploadDocumentRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UploadDocument for user {UserId}", userId);
        return await _grpc.UploadDocumentAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<ListReaderLibraryBooksResponse> ListReaderLibraryBooksAsync(
        string projectId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var request = new ListReaderLibraryBooksRequest { ProjectId = projectId };
        return await _grpc.ListReaderLibraryBooksAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<SaveReaderLibraryBookResponse> SaveReaderLibraryBookAsync(
        SaveReaderLibraryBookRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return await _grpc.SaveReaderLibraryBookAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task DeleteReaderLibraryBookAsync(
        string projectId,
        string bookId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteReaderLibraryBookRequest
        {
            ProjectId = projectId,
            BookId = bookId
        };
        await _grpc.DeleteReaderLibraryBookAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<ListReaderCollectionsResponse> ListReaderCollectionsAsync(
        string projectId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var request = new ListReaderCollectionsRequest { ProjectId = projectId };
        return await _grpc.ListReaderCollectionsAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<SaveReaderCollectionResponse> SaveReaderCollectionAsync(
        SaveReaderCollectionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return await _grpc.SaveReaderCollectionAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task DeleteReaderCollectionAsync(
        string projectId,
        string collectionId,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteReaderCollectionRequest
        {
            ProjectId = projectId,
            CollectionId = collectionId
        };
        await _grpc.DeleteReaderCollectionAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<ShareReaderCollectionResponse> ShareReaderCollectionAsync(
        ShareReaderCollectionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return await _grpc.ShareReaderCollectionAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<UnshareReaderCollectionResponse> UnshareReaderCollectionAsync(
        UnshareReaderCollectionRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return await _grpc.UnshareReaderCollectionAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<ListSharedReaderCollectionsResponse> ListSharedReaderCollectionsAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return await _grpc.ListSharedReaderCollectionsAsync(
            new ListSharedReaderCollectionsRequest(),
            BuildMetadata(userId, roles),
            cancellationToken: cancellationToken);
    }

    public async Task<GetDocumentUrlResponse> GetDocumentUrlAsync(
        GetDocumentUrlRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return await _grpc.GetDocumentUrlAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task PutDocumentExtractAsync(
        PutDocumentExtractRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        await _grpc.PutDocumentExtractAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task<GetDocumentExtractResponse> GetDocumentExtractAsync(
        GetDocumentExtractRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return await _grpc.GetDocumentExtractAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task DeleteDocumentExtractAsync(
        DeleteDocumentExtractRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        await _grpc.DeleteDocumentExtractAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public async Task DeleteProjectMediaAsync(
        DeleteProjectMediaRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        await _grpc.DeleteProjectMediaAsync(request, BuildMetadata(userId, roles), cancellationToken: cancellationToken);
    }
}
