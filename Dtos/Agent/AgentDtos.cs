namespace AggregatorService.Dtos.Agent;

public class AgentThreadListItemDto
{
    public required string Id { get; set; }
    public required string ProjectId { get; set; }
    public required string Title { get; set; }
    public string? AgentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AgentThreadDto : AgentThreadListItemDto
{
    public DateTime? ArchivedAt { get; set; }
}


public class CreateAgentThreadRequestDto
{
    public required string ProjectId { get; set; }
    public string? AgentId { get; set; }
    public string? SystemPromptOverride { get; set; }
}

public class AgentMessageDto
{
    public required string Id { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AgentMessageListDto
{
    public required IReadOnlyList<AgentMessageDto> Items { get; set; }
    public string? NextBefore { get; set; }
}

public class AgentMessageInputDto
{
    public string? Id { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public string? MetadataJson { get; set; }
}

public class AgentDomainDecisionDto
{
    public bool Allowed { get; set; }
    public required string Category { get; set; }
    public string? Reason { get; set; }
}

public class AgentToolCallDto
{
    public required string ToolName { get; set; }
    public required string InputJson { get; set; }
    public required string OutputJson { get; set; }
    public required string Status { get; set; }
}

public class ExecuteAgentRunRequestDto
{
    public required string ProjectId { get; set; }
    public required string UserText { get; set; }
    public string? SourceLang { get; set; }
    public string? TargetLang { get; set; }
    public string? FirstDeckId { get; set; }
    public bool IsInitialGreeting { get; set; }
}

public class CreateAgentRunRequestDto
{
    public required string ProjectId { get; set; }
    public required AgentMessageInputDto UserMessage { get; set; }
    public required AgentMessageInputDto AssistantMessage { get; set; }
    public required AgentDomainDecisionDto DomainDecision { get; set; }
    public required IReadOnlyList<AgentToolCallDto> ToolCalls { get; set; }
    public string? Model { get; set; }
}

public class AgentRunDto
{
    public required string Id { get; set; }
    public required string ThreadId { get; set; }
    public required string Status { get; set; }
    public string? Model { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreateAgentRunResponseDto
{
    public required AgentRunDto Run { get; set; }
    public required AgentMessageDto UserMessage { get; set; }
    public required AgentMessageDto AssistantMessage { get; set; }
}
