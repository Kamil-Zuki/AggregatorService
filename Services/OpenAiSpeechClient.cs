#nullable enable
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AggregatorService.Options;
using Microsoft.Extensions.Options;

namespace AggregatorService.Services;

/// <summary>
/// <c>POST /v1/audio/speech</c> client for OpenAI (binary body) and Mistral Voxtral (JSON + base64).
/// </summary>
public sealed class OpenAiSpeechClient
{
    private readonly HttpClient _http;
    private readonly IOptions<AiCompletionOptions> _options;

    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public OpenAiSpeechClient(HttpClient http, IOptions<AiCompletionOptions> options)
    {
        _http = http;
        _options = options;
    }

    public async Task<byte[]> CreateSpeechAsync(
        string model,
        string input,
        string voice,
        string responseFormat,
        double? speed,
        CancellationToken cancellationToken = default)
    {
        var o = _options.Value;
        if (!o.TtsEnabled)
            throw new InvalidOperationException("TTS is disabled (Ai:TtsEnabled=false).");

        if (TtsProviderHelper.IsEspeakProvider(o))
        {
            return await CreateEspeakSpeechAsync(input, voice, speed, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!o.Enabled)
            throw new InvalidOperationException("AI is disabled (Ai:Enabled=false).");

        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException("AI API key is not configured (Ai:ApiKey).");

        if (TtsProviderHelper.IsMistralProvider(o))
        {
            return await CreateMistralSpeechAsync(model, input, voice, responseFormat, cancellationToken)
                .ConfigureAwait(false);
        }

        return await CreateOpenAiSpeechAsync(model, input, voice, responseFormat, speed, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<byte[]> CreateEspeakSpeechAsync(
        string input,
        string voice,
        double? speed,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(voice) || TtsProviderHelper.IsOpenAiStyleVoiceName(voice))
        {
            throw new InvalidOperationException(
                "Free TTS voice is not configured for espeak-ng. Use AI_TTS_PROVIDER=espeak with language voices (en-us, ru, ko) or leave per-language voices unset.");
        }

        var o = _options.Value;
        var tempFile = Path.Combine(Path.GetTempPath(), $"polyraspad-tts-{Guid.NewGuid():N}.wav");
        try
        {
            var wordsPerMinute = SpeedToWordsPerMinute(speed);
            var startInfo = new ProcessStartInfo
            {
                FileName = string.IsNullOrWhiteSpace(o.TtsEspeakCommand) ? "espeak-ng" : o.TtsEspeakCommand.Trim(),
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            startInfo.ArgumentList.Add("-w");
            startInfo.ArgumentList.Add(tempFile);
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add(voice);
            startInfo.ArgumentList.Add("-s");
            startInfo.ArgumentList.Add(wordsPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add(input);

            using var process = Process.Start(startInfo)
                ?? throw new HttpRequestException("Free TTS command failed to start.");

            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new HttpRequestException(
                    $"Free TTS command failed with exit code {process.ExitCode}: {stderr.Trim()}");
            }

            var bytes = await File.ReadAllBytesAsync(tempFile, cancellationToken).ConfigureAwait(false);
            if (bytes.Length == 0)
                throw new HttpRequestException("Free TTS command returned an empty audio file.");

            return bytes;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException(
                "Free TTS provider requires espeak-ng. Install espeak-ng or run the Aggregator Docker image.",
                ex);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch
            {
                // Temp-file cleanup is best-effort; synthesis result/errors have already been determined.
            }
        }
    }

    private static int SpeedToWordsPerMinute(double? speed)
    {
        var normalized = Math.Clamp(speed ?? 1.0, 0.25, 4.0);
        return (int)Math.Round(175 * normalized);
    }

    private async Task<byte[]> CreateOpenAiSpeechAsync(
        string model,
        string input,
        string voice,
        string responseFormat,
        double? speed,
        CancellationToken cancellationToken)
    {
        var body = new OpenAiSpeechRequest
        {
            Model = model,
            Input = input,
            Voice = voice,
            ResponseFormat = responseFormat,
            Speed = speed,
        };

        using var response = await _http
            .PostAsJsonAsync("audio/speech", body, JsonWrite, cancellationToken)
            .ConfigureAwait(false);

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw CreateHttpException(response.StatusCode, bytes);

        return bytes;
    }

    private async Task<byte[]> CreateMistralSpeechAsync(
        string model,
        string input,
        string voiceId,
        string responseFormat,
        CancellationToken cancellationToken)
    {
        var body = new MistralSpeechRequest
        {
            Model = model,
            Input = input,
            VoiceId = voiceId,
            ResponseFormat = responseFormat,
        };

        using var response = await _http
            .PostAsJsonAsync("audio/speech", body, JsonWrite, cancellationToken)
            .ConfigureAwait(false);

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw CreateHttpException(response.StatusCode, bytes);

        MistralSpeechResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<MistralSpeechResponse>(bytes, JsonRead);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException(
                "TTS returned success but response was not valid Mistral audio JSON.",
                ex);
        }

        if (parsed?.AudioData is not { Length: > 0 } audioB64)
        {
            throw new HttpRequestException("TTS returned success but audio_data was missing or empty.");
        }

        try
        {
            return Convert.FromBase64String(audioB64);
        }
        catch (FormatException ex)
        {
            throw new HttpRequestException("TTS returned invalid base64 in audio_data.", ex);
        }
    }

    private static HttpRequestException CreateHttpException(System.Net.HttpStatusCode statusCode, byte[] bytes)
    {
        var detail = bytes.Length > 0 && bytes.Length < 4096
            ? System.Text.Encoding.UTF8.GetString(bytes)
            : $"HTTP {(int)statusCode}";
        return new HttpRequestException($"TTS HTTP {(int)statusCode}: {detail}");
    }

    private sealed class OpenAiSpeechRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("input")]
        public string Input { get; set; } = "";

        [JsonPropertyName("voice")]
        public string Voice { get; set; } = "";

        [JsonPropertyName("response_format")]
        public string ResponseFormat { get; set; } = "mp3";

        [JsonPropertyName("speed")]
        public double? Speed { get; set; }
    }

    private sealed class MistralSpeechRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("input")]
        public string Input { get; set; } = "";

        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; } = "";

        [JsonPropertyName("response_format")]
        public string ResponseFormat { get; set; } = "mp3";
    }

    private sealed class MistralSpeechResponse
    {
        [JsonPropertyName("audio_data")]
        public string? AudioData { get; set; }
    }
}
