#nullable enable
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AggregatorService.Options;
using Microsoft.Extensions.Options;

namespace AggregatorService.Services;

/// <summary>
/// Minimal OpenAI-compatible <c>POST /v1/chat/completions</c> client.
/// </summary>
public sealed class OpenAiChatCompletionClient
{
    private readonly HttpClient _http;
    private readonly IOptions<AiCompletionOptions> _options;

    private static readonly JsonSerializerOptions JsonParse = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public OpenAiChatCompletionClient(HttpClient http, IOptions<AiCompletionOptions> options)
    {
        _http = http;
        _options = options;
    }

    public async Task<string> CompleteAsync(
        string? modelOverride,
        string systemMessage,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var o = _options.Value;
        if (!o.Enabled)
            throw new InvalidOperationException("AI completion is disabled (Ai:Enabled=false).");

        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException("AI API key is not configured (Ai:ApiKey).");

        var model = o.AllowClientModelOverride && !string.IsNullOrWhiteSpace(modelOverride)
            ? modelOverride!.Trim()
            : (o.Model ?? "").Trim();

        if (string.IsNullOrEmpty(model))
            throw new InvalidOperationException("AI model is not configured (Ai:Model).");

        var body = new ChatCompletionRequest
        {
            Model = model,
            Temperature = 0.3,
            Messages =
            [
                new ChatMessage { Role = "system", Content = systemMessage },
                new ChatMessage { Role = "user", Content = userMessage },
            ],
        };

        using var response = await _http.PostAsJsonAsync("chat/completions", body, cancellationToken).ConfigureAwait(false);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"LLM HTTP {(int)response.StatusCode}: {raw}");

        ChatCompletionResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(raw, JsonParse);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Invalid JSON from chat/completions.", ex);
        }

        var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("LLM returned empty message content.");

        return content;
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private sealed class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
