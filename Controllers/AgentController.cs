using AggregatorService.Dtos.Agent;
using AggregatorService.Helpers;
using AggregatorService.Services;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pvs.Agent.Grpc;
using System.Text.Json.Nodes;
using Pvs.Content.Grpc;

namespace AggregatorService.Controllers;

[ApiController]
[Route("api/agent")]
[Authorize]
[AggregatorService.Filters.FeatureFlagFilter("EnableAIAgents")]
public class AgentController : ControllerBase
{
    private readonly IAgentServiceClient _agentServiceClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentController> _logger;
    private readonly IMapper _mapper;

    public AgentController(
        IAgentServiceClient agentServiceClient,
        IServiceScopeFactory scopeFactory,
        ILogger<AgentController> logger,
        IMapper mapper)
    {
        _agentServiceClient = agentServiceClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpGet("threads")]
    [ProducesResponseType(typeof(IReadOnlyList<AgentThreadListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgentThreadListItemDto>>> ListThreads(
        [FromQuery] string projectId,
        [FromQuery] string? agentId = null)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return BadRequest(new { error = "projectId is required" });

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new ListAgentThreadsRequest { ProjectId = projectId.Trim() };
            if (!string.IsNullOrWhiteSpace(agentId))
                grpcRequest.AgentId = agentId.Trim();

            var grpcResponse = await _agentServiceClient.ListAgentThreadsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var items = grpcResponse.Items
                .Select(item => _mapper.Map<AgentThreadListItemDto>(item))
                .ToList();

            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListAgentThreads failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("threads")]
    [ProducesResponseType(typeof(AgentThreadDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AgentThreadDto>> CreateThread([FromBody] CreateAgentThreadRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectId))
            return BadRequest(new { error = "projectId is required" });

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new CreateAgentThreadRequest { ProjectId = request.ProjectId.Trim() };
            if (!string.IsNullOrWhiteSpace(request.AgentId))
                grpcRequest.AgentId = request.AgentId.Trim();
            if (!string.IsNullOrWhiteSpace(request.SystemPromptOverride))
                grpcRequest.SystemPromptOverride = request.SystemPromptOverride.Trim();

            var grpcResponse = await _agentServiceClient.CreateAgentThreadAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var dto = _mapper.Map<AgentThreadDto>(grpcResponse);
            return CreatedAtAction(nameof(GetThread), new { threadId = dto.Id }, dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAgentThread failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpGet("threads/{threadId}")]
    [ProducesResponseType(typeof(AgentThreadDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentThreadDto>> GetThread(string threadId)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcResponse = await _agentServiceClient.GetAgentThreadAsync(
                new GetAgentThreadRequest { ThreadId = threadId },
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(_mapper.Map<AgentThreadDto>(grpcResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAgentThread failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpGet("threads/{threadId}/messages")]
    [ProducesResponseType(typeof(AgentMessageListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentMessageListDto>> ListMessages(
        string threadId,
        [FromQuery] int limit = 100,
        [FromQuery] string? before = null)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new ListAgentMessagesRequest
            {
                ThreadId = threadId,
                Limit = limit
            };

            if (!string.IsNullOrWhiteSpace(before))
                grpcRequest.Before = before.Trim();

            var grpcResponse = await _agentServiceClient.ListAgentMessagesAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(new AgentMessageListDto
            {
                Items = grpcResponse.Items.Select(item => _mapper.Map<AgentMessageDto>(item)).ToList(),
                NextBefore = string.IsNullOrEmpty(grpcResponse.NextBefore) ? null : grpcResponse.NextBefore
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListAgentMessages failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("threads/{threadId}/runs")]
    [ProducesResponseType(typeof(CreateAgentRunResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateAgentRunResponseDto>> CreateRun(
        string threadId,
        [FromBody] ExecuteAgentRunRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectId))
            return BadRequest(new { error = "projectId is required" });

        if (string.IsNullOrWhiteSpace(request.UserText))
            return BadRequest(new { error = "userText is required" });

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new ExecuteAgentRunRequest
            {
                ThreadId = threadId,
                ProjectId = request.ProjectId.Trim(),
                UserText = request.UserText.Trim()
            };

            if (!string.IsNullOrWhiteSpace(request.SourceLang))
                grpcRequest.SourceLang = request.SourceLang.Trim();

            if (!string.IsNullOrWhiteSpace(request.TargetLang))
                grpcRequest.TargetLang = request.TargetLang.Trim();

            if (!string.IsNullOrWhiteSpace(request.FirstDeckId))
                grpcRequest.FirstDeckId = request.FirstDeckId.Trim();

            if (request.IsInitialGreeting)
                grpcRequest.IsInitialGreeting = true;

            var grpcResponse = await _agentServiceClient.ExecuteAgentRunAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            if (!string.IsNullOrWhiteSpace(grpcResponse.AssistantMessage?.MetadataJson))
            {
                var metadataStr = grpcResponse.AssistantMessage.MetadataJson;
                var targetLangStr = request.TargetLang;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var ttsService = scope.ServiceProvider.GetRequiredService<ITtsAudioService>();
                        var vocabService = scope.ServiceProvider.GetRequiredService<IVocabularyServiceClient>();
                        
                        var metadata = JsonNode.Parse(metadataStr);
                        var toolCalls = metadata?["toolCalls"]?.AsArray();
                        if (toolCalls != null)
                        {
                            foreach (var tc in toolCalls)
                            {
                                if (tc["name"]?.ToString() == "create_card" && tc["status"]?.ToString() == "completed")
                                {
                                    var resultJson = tc["result"]?.ToString();
                                    var inputJson = tc["input"]?.ToString();
                                    if (!string.IsNullOrEmpty(resultJson) && !string.IsNullOrEmpty(inputJson))
                                    {
                                        var resultNode = JsonNode.Parse(resultJson);
                                        var cardIdStr = resultNode?["Id"]?.ToString();
                                        
                                        var inputNode = JsonNode.Parse(inputJson);
                                        var word = inputNode?["word"]?.ToString();

                                        if (!string.IsNullOrEmpty(cardIdStr) && !string.IsNullOrEmpty(word))
                                        {
                                            var lang = !string.IsNullOrWhiteSpace(targetLangStr) ? targetLangStr.Trim() : "en";
                                            var audioDto = await ttsService.GenerateAndStoreAsync(new Dtos.GenerateAudioRequestDto
                                            {
                                                Text = word,
                                                Language = lang
                                            }, userId, roles, default).ConfigureAwait(false);

                                            if (!string.IsNullOrEmpty(audioDto.Url))
                                            {
                                                await vocabService.UpdateCardAsync(new UpdateCardRequest
                                                {
                                                    CardId = cardIdStr,
                                                    FieldValues = { ["Audio"] = new NoteFieldValuePayload { StringValue = audioDto.Url } }
                                                }, userId, roles, default).ConfigureAwait(false);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Background TTS generation failure should not crash the process
                    }
                });
            }

            return Ok(new CreateAgentRunResponseDto
            {
                Run = _mapper.Map<AgentRunDto>(grpcResponse.Run),
                UserMessage = _mapper.Map<AgentMessageDto>(grpcResponse.UserMessage),
                AssistantMessage = _mapper.Map<AgentMessageDto>(grpcResponse.AssistantMessage)
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExecuteAgentRun failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("threads/{threadId}/runs/stream")]
    public async Task ExecuteRunStream(
        string threadId,
        [FromBody] ExecuteAgentRunRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectId))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (string.IsNullOrWhiteSpace(request.UserText))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new ExecuteAgentRunRequest
            {
                ThreadId = threadId,
                ProjectId = request.ProjectId.Trim(),
                UserText = request.UserText.Trim()
            };

            if (!string.IsNullOrWhiteSpace(request.SourceLang))
                grpcRequest.SourceLang = request.SourceLang.Trim();

            if (!string.IsNullOrWhiteSpace(request.TargetLang))
                grpcRequest.TargetLang = request.TargetLang.Trim();

            if (!string.IsNullOrWhiteSpace(request.FirstDeckId))
                grpcRequest.FirstDeckId = request.FirstDeckId.Trim();

            if (request.IsInitialGreeting)
                grpcRequest.IsInitialGreeting = true;

            using var call = _agentServiceClient.ExecuteAgentRunStreamAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            await foreach (var response in call.ResponseStream.ReadAllAsync(HttpContext.RequestAborted))
            {
                var eventName = response.EventCase switch
                {
                    ExecuteAgentRunStreamResponse.EventOneofCase.ContentChunk => "chunk",
                    ExecuteAgentRunStreamResponse.EventOneofCase.ToolCall => "tool_call",
                    ExecuteAgentRunStreamResponse.EventOneofCase.FinalResult => "final_result",
                    ExecuteAgentRunStreamResponse.EventOneofCase.Error => "error",
                    _ => "unknown"
                };

                var data = response.EventCase switch
                {
                    ExecuteAgentRunStreamResponse.EventOneofCase.ContentChunk => System.Text.Json.JsonSerializer.Serialize(new { chunk = response.ContentChunk }),
                    ExecuteAgentRunStreamResponse.EventOneofCase.ToolCall => System.Text.Json.JsonSerializer.Serialize(new { toolCall = response.ToolCall }),
                    ExecuteAgentRunStreamResponse.EventOneofCase.FinalResult => System.Text.Json.JsonSerializer.Serialize(new { 
                        run = _mapper.Map<AgentRunDto>(response.FinalResult.Run),
                        userMessage = _mapper.Map<AgentMessageDto>(response.FinalResult.UserMessage),
                        assistantMessage = _mapper.Map<AgentMessageDto>(response.FinalResult.AssistantMessage)
                    }),
                    ExecuteAgentRunStreamResponse.EventOneofCase.Error => System.Text.Json.JsonSerializer.Serialize(new { error = response.Error }),
                    _ => "{}"
                };

                await Response.WriteAsync($"event: {eventName}\n");
                await Response.WriteAsync($"data: {data}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (RpcException ex)
        {
            await Response.WriteAsync($"event: error\n");
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = ex.Status.Detail })}\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExecuteAgentRunStream failed");
            await Response.WriteAsync($"event: error\n");
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error" })}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    /// <summary>
    /// Persist a user + assistant message pair (and optional tool calls) without invoking the LLM.
    /// Used by open-agent workspaces to store clarification and execution-summary messages.
    /// </summary>
    [HttpPost("threads/{threadId}/runs/persist")]
    [ProducesResponseType(typeof(CreateAgentRunResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateAgentRunResponseDto>> PersistRun(
        string threadId,
        [FromBody] CreateAgentRunRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectId))
            return BadRequest(new { error = "projectId is required" });

        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var grpcRequest = new CreateAgentRunRequest
            {
                UserId = userId.ToString(),
                ThreadId = threadId,
                ProjectId = request.ProjectId.Trim(),
                UserMessage = MapMessageInput(request.UserMessage),
                AssistantMessage = MapMessageInput(request.AssistantMessage),
                DomainDecision = new AgentDomainDecisionInput
                {
                    Allowed = request.DomainDecision.Allowed,
                    Category = request.DomainDecision.Category,
                    Reason = request.DomainDecision.Reason ?? string.Empty
                },
                Model = request.Model ?? string.Empty
            };

            foreach (var toolCall in request.ToolCalls)
            {
                grpcRequest.ToolCalls.Add(new AgentToolCallInput
                {
                    ToolName = toolCall.ToolName,
                    InputJson = toolCall.InputJson,
                    OutputJson = toolCall.OutputJson,
                    Status = toolCall.Status
                });
            }

            var grpcResponse = await _agentServiceClient.CreateAgentRunAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            return Ok(new CreateAgentRunResponseDto
            {
                Run = _mapper.Map<AgentRunDto>(grpcResponse.Run),
                UserMessage = _mapper.Map<AgentMessageDto>(grpcResponse.UserMessage),
                AssistantMessage = _mapper.Map<AgentMessageDto>(grpcResponse.AssistantMessage)
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PersistAgentRun failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    [HttpPost("threads/{threadId}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ArchiveThread(string threadId)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            await _agentServiceClient.ArchiveAgentThreadAsync(
                new ArchiveAgentThreadRequest { ThreadId = threadId },
                userId,
                roles,
                HttpContext.RequestAborted);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            return MapRpc(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ArchiveAgentThread failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    private static AgentMessageInput MapMessageInput(AgentMessageInputDto dto)
    {
        var input = new AgentMessageInput
        {
            Role = dto.Role,
            Content = dto.Content
        };

        if (!string.IsNullOrWhiteSpace(dto.Id))
            input.Id = dto.Id.Trim();

        if (!string.IsNullOrWhiteSpace(dto.MetadataJson))
            input.MetadataJson = dto.MetadataJson.Trim();

        return input;
    }

    private ActionResult MapRpc(RpcException ex)
    {
        if (AggregatorService.Helpers.BillingLimitHttp.TryHandleRpcException(ex, out var limitResult))
        {
            return limitResult;
        }

        var statusCode = ex.StatusCode switch
        {
            Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
            Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
            Grpc.Core.StatusCode.FailedPrecondition => StatusCodes.Status409Conflict,
            Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new { error = ex.Status.Detail });
    }
}
