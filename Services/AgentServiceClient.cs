using Grpc.Core;
using Pvs.Agent.Grpc;
using GrpcAgentServiceClient = Pvs.Agent.Grpc.AgentService.AgentServiceClient;

namespace AggregatorService.Services;

public class AgentServiceClient : IAgentServiceClient
{
    private readonly GrpcAgentServiceClient _grpcClient;
    private readonly ILogger<AgentServiceClient> _logger;

    public AgentServiceClient(GrpcAgentServiceClient grpcClient, ILogger<AgentServiceClient> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
    }

    public Task<ListAgentThreadsResponse> ListAgentThreadsAsync(
        ListAgentThreadsRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _grpcClient.ListThreadsAsync(request, CreateMetadata(userId, roles), cancellationToken: cancellationToken)
            .ResponseAsync;
    }

    public Task<AgentThreadResponse> CreateAgentThreadAsync(
        CreateAgentThreadRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _grpcClient.CreateThreadAsync(request, CreateMetadata(userId, roles), cancellationToken: cancellationToken)
            .ResponseAsync;
    }

    public Task<AgentThreadResponse> GetAgentThreadAsync(
        GetAgentThreadRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _grpcClient.GetThreadAsync(request, CreateMetadata(userId, roles), cancellationToken: cancellationToken)
            .ResponseAsync;
    }

    public Task<ListAgentMessagesResponse> ListAgentMessagesAsync(
        ListAgentMessagesRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _grpcClient.ListMessagesAsync(request, CreateMetadata(userId, roles), cancellationToken: cancellationToken)
            .ResponseAsync;
    }

    public Task<CreateAgentRunResponse> CreateAgentRunAsync(
        CreateAgentRunRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _grpcClient.CreateRunAsync(request, CreateMetadata(userId, roles), cancellationToken: cancellationToken)
            .ResponseAsync;
    }

    public Task<CreateAgentRunResponse> ExecuteAgentRunAsync(
        ExecuteAgentRunRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _grpcClient.ExecuteRunAsync(request, CreateMetadata(userId, roles), cancellationToken: cancellationToken)
            .ResponseAsync;
    }

    public async Task ArchiveAgentThreadAsync(
        ArchiveAgentThreadRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        await _grpcClient.ArchiveThreadAsync(request, CreateMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    public Grpc.Core.AsyncServerStreamingCall<ExecuteAgentRunStreamResponse> ExecuteAgentRunStreamAsync(
        ExecuteAgentRunRequest request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        request.UserId = userId.ToString();
        return _grpcClient.ExecuteRunStream(request, CreateMetadata(userId, roles), cancellationToken: cancellationToken);
    }

    private static Metadata CreateMetadata(Guid userId, IEnumerable<string> roles) => new()
    {
        { "user_id", userId.ToString() },
        { "roles", string.Join(",", roles) }
    };
}
