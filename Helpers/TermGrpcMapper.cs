using AggregatorService.Dtos;
using Pvs.Content.Grpc;

namespace AggregatorService.Helpers;

/// <summary>
/// Маппинг REST DTO терминов ↔ gRPC (term-first: term_text, type WORD|PHRASE).
/// </summary>
public static class TermGrpcMapper
{
    public static CreateOrUpdateTermRequest ToCreateOrUpdateRequest(CreateOrUpdateTermDto dto, Guid userId)
    {
        var r = new CreateOrUpdateTermRequest
        {
            UserId = userId.ToString(),
            ProjectId = dto.ProjectId,
            TermText = dto.TermText,
            Type = dto.Type,
            Language = dto.Language ?? string.Empty,
            Status = dto.Status,
            Meaning = dto.Meaning ?? string.Empty,
            FirstSentence = dto.FirstSentence ?? string.Empty,
            FirstSourceTitle = dto.FirstSourceTitle ?? string.Empty,
            FirstSourceUrl = dto.FirstSourceUrl ?? string.Empty
        };

        return r;
    }

    public static TermActionRequest ToTermActionRequest(TermActionDto dto, Guid userId)
    {
        return new TermActionRequest
        {
            UserId = userId.ToString(),
            ProjectId = dto.ProjectId,
            TermText = dto.TermText,
            Type = dto.Type,
            Language = dto.Language ?? string.Empty
        };
    }

    public static BulkMarkKnownRequest ToBulkMarkKnownRequest(BulkMarkKnownDto dto, Guid userId)
    {
        var r = new BulkMarkKnownRequest
        {
            UserId = userId.ToString(),
            ProjectId = dto.ProjectId,
            Language = dto.Language ?? string.Empty
        };
        if (dto.Items.Count > 0)
        {
            foreach (var item in dto.Items)
            {
                r.Items.Add(new BulkMarkKnownItem
                {
                    TermText = item.TermText,
                    Type = item.Type ?? "WORD",
                });
            }
        }
        else
        {
            r.TermTexts.AddRange(dto.TermTexts);
        }

        return r;
    }

    public static GetTermDetailsRequest ToGetTermDetailsRequest(string projectId, string termText, string type, Guid userId)
    {
        return new GetTermDetailsRequest
        {
            UserId = userId.ToString(),
            ProjectId = projectId,
            TermText = termText,
            Type = type
        };
    }

    public static SearchTermDuplicatesRequest ToSearchDuplicatesRequest(SearchTermDuplicatesDto dto, Guid userId)
    {
        return new SearchTermDuplicatesRequest
        {
            UserId = userId.ToString(),
            ProjectId = dto.ProjectId,
            TermText = dto.TermText,
            Type = dto.Type
        };
    }

    public static TermDetailsDto ToTermDetailsDto(TermDetailsResponse grpc)
    {
        return new TermDetailsDto
        {
            TermId = grpc.TermId,
            ProjectId = grpc.ProjectId,
            TermText = grpc.TermText,
            NormalizedText = grpc.NormalizedText,
            Type = grpc.Type,
            Language = grpc.Language,
            Status = grpc.Status,
            Meaning = string.IsNullOrEmpty(grpc.Meaning) ? null : grpc.Meaning,
            FirstSentence = string.IsNullOrEmpty(grpc.FirstSentence) ? null : grpc.FirstSentence,
            FirstSourceTitle = string.IsNullOrEmpty(grpc.FirstSourceTitle) ? null : grpc.FirstSourceTitle,
            FirstSourceUrl = string.IsNullOrEmpty(grpc.FirstSourceUrl) ? null : grpc.FirstSourceUrl,
            RelatedCards = grpc.RelatedCards.Select(ToCardPreviewDto).ToList(),
            ReadingLevel = grpc.ReadingLevel,
            ListeningLevel = grpc.ListeningLevel,
            WritingLevel = grpc.WritingLevel,
            SpeakingLevel = grpc.SpeakingLevel
        };
    }

    public static SearchTermDuplicatesResponseDto ToSearchDuplicatesDto(SearchTermDuplicatesResponse grpc)
    {
        return new SearchTermDuplicatesResponseDto
        {
            IsDuplicate = grpc.IsDuplicate,
            NormalizedText = grpc.NormalizedText,
            MatchingTerms = grpc.MatchingTerms.Select(ToTermDetailsDto).ToList(),
            ExistingCards = grpc.ExistingCards.Select(ToCardPreviewDto).ToList()
        };
    }

    public static ListProjectTermsRequest ToListProjectTermsRequest(
        string projectId,
        string? status,
        string? type,
        string? q,
        int pageNumber,
        int? pageSize,
        Guid userId)
    {
        return new ListProjectTermsRequest
        {
            UserId = userId.ToString("D"),
            ProjectId = projectId,
            Status = status ?? string.Empty,
            Type = type ?? string.Empty,
            Q = q ?? string.Empty,
            PageNumber = pageNumber,
            PageSize = pageSize ?? 0,
        };
    }

    public static ListProjectTermsResponseDto ToListProjectTermsResponseDto(ListProjectTermsResponse grpc)
    {
        var dto = new ListProjectTermsResponseDto
        {
            Items = grpc.Items.Select(static item => new ProjectTermListItemDto
            {
                TermId = item.TermId,
                Text = item.Text,
                NormalizedText = item.NormalizedText,
                Type = item.Type,
                Language = item.Language,
                Status = item.Status,
                Meaning = string.IsNullOrEmpty(item.Meaning) ? null : item.Meaning,
                FirstSentence = string.IsNullOrEmpty(item.FirstSentence) ? null : item.FirstSentence,
                FirstSourceTitle = string.IsNullOrEmpty(item.FirstSourceTitle) ? null : item.FirstSourceTitle,
                FirstSourceUrl = string.IsNullOrEmpty(item.FirstSourceUrl) ? null : item.FirstSourceUrl,
                UpdatedAt = item.UpdatedAt.ToDateTimeOffset(),
                RelatedCardCount = item.RelatedCardCount,
                ReadingLevel = item.ReadingLevel,
                ListeningLevel = item.ListeningLevel,
                WritingLevel = item.WritingLevel,
                SpeakingLevel = item.SpeakingLevel,
            }).ToList(),
        };

        dto.TotalCount = grpc.TotalCount;

        return dto;
    }

    private static CardPreviewDto ToCardPreviewDto(CardPreview grpc)
    {
        var dto = new CardPreviewDto
        {
            Id = grpc.Id,
            SrsStatus = grpc.SrsStatus.ToString(),
            HasAudio = grpc.HasAudio,
            DeckTitle = grpc.DeckTitle
        };
        if (grpc.Note == null) return dto;
        dto.Note = new NotePayloadDto
        {
            Id = grpc.Note.Id,
            NoteTypeId = grpc.Note.NoteTypeId,
        };
        if (!string.IsNullOrEmpty(grpc.Note.ProjectTermId))
            dto.Note.ProjectTermId = grpc.Note.ProjectTermId;
        foreach (var kv in grpc.Note.FieldValues)
        {
            dto.Note.FieldValues[kv.Key] = new NoteFieldValueDto
            {
                StringValue = string.IsNullOrEmpty(kv.Value.StringValue) ? null : kv.Value.StringValue,
                StringValues = kv.Value.StringValues.Count > 0 ? kv.Value.StringValues.ToList() : null,
            };
        }

        return dto;
    }
}
