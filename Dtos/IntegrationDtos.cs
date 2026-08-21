using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos;

public sealed class IntegrationProviderOptionDto
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

public sealed class IntegrationProvidersResponseDto
{
    public List<IntegrationProviderOptionDto> Translators { get; set; } = [];

    public List<IntegrationProviderOptionDto> Dictionaries { get; set; } = [];
}

public sealed class TranslateRequestDto
{
    [Required]
    public string Text { get; set; } = string.Empty;

    [Required]
    [StringLength(16)]
    public string SourceLanguage { get; set; } = "en";

    [Required]
    [StringLength(16)]
    public string TargetLanguage { get; set; } = "ru";

    [Required]
    [StringLength(64)]
    public string Provider { get; set; } = "mymemory";
}

public sealed class TranslateResponseDto
{
    public string Provider { get; set; } = string.Empty;

    public string TranslatedText { get; set; } = string.Empty;
}

public sealed class DictionaryLookupRequestDto
{
    [Required]
    [StringLength(128)]
    public string Word { get; set; } = string.Empty;

    [Required]
    [StringLength(16)]
    public string Language { get; set; } = "en";

    [Required]
    [StringLength(64)]
    public string Provider { get; set; } = "freedictionary";
}

public sealed class DictionaryLookupResponseDto
{
    public string Provider { get; set; } = string.Empty;

    public string Word { get; set; } = string.Empty;

    public string? Phonetic { get; set; }

    public List<DictionaryMeaningDto> Meanings { get; set; } = [];
}

public sealed class DictionaryMeaningDto
{
    public string PartOfSpeech { get; set; } = string.Empty;

    public List<string> Definitions { get; set; } = [];
}
