#nullable enable
using System.Text.Json;
using AggregatorService.Dtos;
using AggregatorService.Options;
using Microsoft.Extensions.Options;

namespace AggregatorService.Services;

/// <summary>
/// Study session copilot feedback via OpenAI-compatible chat API.
/// </summary>
public sealed class OpenAiCompatibleStudyCopilotFeedbackService : IStudyCopilotFeedbackService
{
    private readonly OpenAiChatCompletionClient _chat;
    private readonly ILogger<OpenAiCompatibleStudyCopilotFeedbackService> _logger;
    private readonly AiCompletionOptions _opts;

    private static readonly JsonSerializerOptions JsonParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public OpenAiCompatibleStudyCopilotFeedbackService(
        OpenAiChatCompletionClient chat,
        IOptions<AiCompletionOptions> options,
        ILogger<OpenAiCompatibleStudyCopilotFeedbackService> logger)
    {
        _chat = chat;
        _logger = logger;
        _opts = options.Value;
    }

    public async Task<CopilotReviewFeedbackDto> GetFeedbackAsync(
        CopilotReviewFeedbackRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_opts.Enabled || string.IsNullOrWhiteSpace(_opts.ApiKey))
            return NeutralFallback();

        var ratingLabel = request.Rating switch
        {
            1 => "Again (forgot)",
            2 => "Hard",
            3 => "Good",
            4 => "Easy",
            _ => $"Rating {request.Rating}",
        };

        var userPart = string.IsNullOrWhiteSpace(request.UserAnswer)
            ? "(no typed answer)"
            : request.UserAnswer;

        var system =
            "You help a vocabulary learner. Be concise. Output ONLY valid JSON, no markdown fences, no extra text. " +
            "Schema: {\"tone\":\"encouraging|neutral|firm\",\"explanation\":\"1-3 sentences\",\"actionHint\":\"short tip\","
            + "\"suggestRemedialCards\":boolean,\"remedialCards\":[{"
            + "\"sentence\":\"\",\"targetWord\":\"\",\"translation\":\"\"}]}";
        var user =
            $"Context: sentence=\"{EscapeForPrompt(request.Sentence)}\", target=\"{EscapeForPrompt(request.TargetWord)}\", "
            + $"translation=\"{EscapeForPrompt(request.Translation)}\", user answer=\"{EscapeForPrompt(userPart)}\", "
            + $"SRS rating={request.Rating} ({ratingLabel}). "
            + "Give brief feedback on recall quality and one learning tip. remedialCards only if suggestRemedialCards is true.";

        try
        {
            var content = await _chat.CompleteAsync(modelOverride: null, system, user, cancellationToken).ConfigureAwait(false);
            var jsonPayload = ExtractJsonObject(content);
            var dto = JsonSerializer.Deserialize<CopilotReviewFeedbackDto>(jsonPayload, JsonParseOptions);
            if (dto == null)
                return NeutralFallback();

            dto.Tone = string.IsNullOrWhiteSpace(dto.Tone) ? "neutral" : dto.Tone.Trim();
            dto.Explanation ??= string.Empty;
            dto.ActionHint ??= string.Empty;
            dto.RemedialCards ??= [];
            return dto;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Study Copilot LLM call failed; returning neutral fallback");
            return NeutralFallback();
        }
    }

    private static string EscapeForPrompt(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\"", "'", StringComparison.Ordinal).Replace('\n', ' ').Replace('\r', ' ');
    }

    private static string ExtractJsonObject(string text)
    {
        var t = UnwrapMarkdownFences(text.Trim());
        var i = t.IndexOf('{');
        var j = t.LastIndexOf('}');
        if (i >= 0 && j > i)
            return t[i..(j + 1)];
        return t;
    }

    private static string UnwrapMarkdownFences(string text)
    {
        var start = text.IndexOf("```", StringComparison.Ordinal);
        if (start < 0) return text;
        var afterOpen = text.IndexOf('\n', start);
        if (afterOpen < 0) return text;
        afterOpen++;
        var end = text.LastIndexOf("```", StringComparison.Ordinal);
        if (end <= afterOpen) return text;
        return text[afterOpen..end].Trim();
    }

    private static CopilotReviewFeedbackDto NeutralFallback() => new()
    {
        Tone = "neutral",
        Explanation = string.Empty,
        ActionHint = string.Empty,
        SuggestRemedialCards = false,
        RemedialCards = [],
    };
}
