#nullable enable
using AggregatorService.Dtos;

namespace AggregatorService.Services;

public interface ITtsAudioService
{
    Task<GenerateAudioResponseDto> GenerateAndStoreAsync(
        GenerateAudioRequestDto request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}
