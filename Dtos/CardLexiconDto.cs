namespace AggregatorService.Dtos;

/// <summary>Optional dictionary-style fields (editor / InOriginal-style capture).</summary>
public class CardLexiconDto
{
    public string? Transcription { get; set; }
    public string? WordTypes { get; set; }
    public string? Definition { get; set; }
    public string? Example { get; set; }
    public string? Antonyms { get; set; }
    public string? Notes { get; set; }
}
