#nullable enable

namespace AggregatorService.Dtos;

public sealed class MiningDraftRequestDto
{
    /// <summary>Full sentence containing the target.</summary>
    public string Sentence { get; set; } = "";

    /// <summary>Surface form as selected in the reader (word or phrase).</summary>
    public string Target { get; set; } = "";

    public string SourceLanguage { get; set; } = "en";

    public string TargetLanguage { get; set; } = "ru";
}

public sealed class MiningDraftResponseDto
{
    /// <summary>Short target translation in context of the sentence (for the flashcard back / meaning).</summary>
    public string TargetTranslationInContext { get; set; } = "";

    /// <summary>Translation of the full sentence (auxiliary context for the learner).</summary>
    public string SentenceTranslation { get; set; } = "";

    /// <summary>Optional dictionary lemma as a hint only; term identity stays the surface form.</summary>
    public string? DictionaryLemmaHint { get; set; }
}
