namespace AggregatorService.Dtos;

/// <summary>Метаданные источника (reader, study).</summary>
public class SourceMetaDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int? Page { get; set; }
    public int? Timestamp { get; set; }
    public string? Service { get; set; }
}

/// <summary>Разрешённые медиа URL/id для study / legacy gRPC CardStudy.</summary>
public class CardMediaDto
{
    public string? ImageId { get; set; }
    public string? AudioId { get; set; }
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
}

/// <summary>Индекс целевого слова в предложении (study proto).</summary>
public class TargetIndexDto
{
    public int Start { get; set; }
    public int Len { get; set; }
}
