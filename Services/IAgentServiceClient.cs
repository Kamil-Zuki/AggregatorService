using Pvs.Agent.Grpc;

namespace AggregatorService.Services;

public interface IAgentServiceClient
{
    Task<ListAgentThreadsResponse> ListAgentThreadsAsync(
        ListAgentThreadsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<AgentThreadResponse> CreateAgentThreadAsync(
        CreateAgentThreadRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<AgentThreadResponse> GetAgentThreadAsync(
        GetAgentThreadRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<ListAgentMessagesResponse> ListAgentMessagesAsync(
        ListAgentMessagesRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<CreateAgentRunResponse> CreateAgentRunAsync(
        CreateAgentRunRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<CreateAgentRunResponse> ExecuteAgentRunAsync(
        ExecuteAgentRunRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task ArchiveAgentThreadAsync(
        ArchiveAgentThreadRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Grpc.Core.AsyncServerStreamingCall<ExecuteAgentRunStreamResponse> ExecuteAgentRunStreamAsync(
        ExecuteAgentRunRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}
