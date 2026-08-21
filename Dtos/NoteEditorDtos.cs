namespace AggregatorService.Dtos;

/// <summary>One slot in an Anki-like note field map.</summary>
public class NoteFieldValueDto
{
    public string? StringValue { get; set; }
    public List<string>? StringValues { get; set; }
}

public class NotePayloadDto
{
    public string Id { get; set; } = string.Empty;

    public string NoteTypeId { get; set; } = string.Empty;

    public Dictionary<string, NoteFieldValueDto> FieldValues { get; set; } = new();

    public string? ProjectTermId { get; set; }
}

public class CardTemplateDto
{
    public string Id { get; set; } = string.Empty;

    public string TemplateKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FrontTemplate { get; set; } = string.Empty;

    public string BackTemplate { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool Enabled { get; set; }
}

public class NoteFieldDefinitionDto
{
    public string FieldKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string FieldType { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool Required { get; set; }

    public bool Archived { get; set; }
}

public class NoteTypeForEditorDto
{
    public string Id { get; set; } = string.Empty;

    public string ProjectId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Version { get; set; }

    public List<NoteFieldDefinitionDto> Fields { get; set; } = new();

    public List<CardTemplateDto> Templates { get; set; } = new();
}

public class GetNoteTypeForEditorResponseDto
{
    public NoteTypeForEditorDto NoteType { get; set; } = null!;

    public CardTemplateDto? DefaultTemplate { get; set; }
}
