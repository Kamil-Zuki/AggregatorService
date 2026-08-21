using AggregatorService.Dtos.Reader;
using Pvs.Content.Grpc;

namespace AggregatorService.Helpers;

/// <summary>
/// Маппинг ответа AnalyzeText (gRPC) в контракт REST reader.
/// </summary>
public static class ReaderTextMapper
{
    public static TextAnalyzeResponseDto ToHttpResponse(AnalyzeTextResponse grpc)
    {
        var tokens = grpc.Tokens.Select(MapToken).ToList();
        var stats = grpc.Stats ?? new TextAnalysisStats();
        var phrases = grpc.Phrases.Select(MapPhrase).ToList();

        return new TextAnalyzeResponseDto
        {
            Tokens = tokens,
            Phrases = phrases,
            Stats = new TextAnalyzeStatsDto
            {
                UniqueWords = stats.UniqueWords,
                KnownPercentage = stats.KnownPercentage,
                NewWordsCount = stats.NewWordsCount,
                LearningWordsCount = stats.LearningWordsCount,
                KnownWordsCount = stats.KnownWordsCount
            }
        };
    }

    private static TextTokenDto MapToken(TextToken t)
    {
        var typeStr = t.Type switch
        {
            TokenType.Word => "WORD",
            TokenType.Space => "SPACE",
            TokenType.Punctuation => "PUNCTUATION",
            _ => null
        };

        var statusStr = t.Status switch
        {
            TokenStatus.New => "NEW",
            TokenStatus.Learning => "LEARNING",
            TokenStatus.Known => "KNOWN",
            TokenStatus.Ignored => "IGNORED",
            _ => typeStr is "SPACE" or "PUNCTUATION" ? "NONE" : "NEW"
        };

        var termText = string.IsNullOrEmpty(t.TermText) ? null : t.TermText;
        var projectTermId = string.IsNullOrEmpty(t.ProjectTermId) ? null : t.ProjectTermId;

        return new TextTokenDto
        {
            Text = t.Text,
            TermText = termText,
            Status = statusStr,
            Type = typeStr,
            ProjectTermId = projectTermId,
        };
    }

    private static TextPhraseDto MapPhrase(TextPhrase p)
    {
        var statusStr = p.Status switch
        {
            TokenStatus.New => "NEW",
            TokenStatus.Learning => "LEARNING",
            TokenStatus.Known => "KNOWN",
            TokenStatus.Ignored => "IGNORED",
            _ => "NEW"
        };

        return new TextPhraseDto
        {
            StartIndex = p.StartIndex,
            EndIndex = p.EndIndex,
            Text = p.Text,
            Status = statusStr,
            ProjectTermId = string.IsNullOrEmpty(p.ProjectTermId) ? null : p.ProjectTermId,
        };
    }
}
