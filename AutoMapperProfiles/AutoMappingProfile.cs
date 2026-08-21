using AggregatorService.Dtos;
using AggregatorService.Dtos.Auth;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Pvs.Auth.Grpc;
using Pvs.Content.Grpc;
using Pvs.Agent.Grpc;

namespace AggregatorService.Mappers;

/// <summary>
/// AutoMapper профиль для маппинга между REST DTO и gRPC сообщениями в AggregatorService
/// </summary>
public class AutoMappingProfile : Profile
{
    public AutoMappingProfile()
    {
        // ========== Проекты ==========

        // CreateProjectDto (REST) -> CreateProjectRequest (gRPC)
        CreateMap<CreateProjectDto, CreateProjectRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore());

        // ProjectResponse (gRPC) -> ProjectResponseDto (REST)
        CreateMap<ProjectResponse, ProjectResponseDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()));

        // SrsSettings (gRPC) -> SrsSettingsDto (REST)
        CreateMap<SrsSettings, SrsSettingsDto>()
            .ForMember(dest => dest.W, opt => opt.MapFrom(src => src.W.ToArray()));

        // SrsSettingsDto (REST) -> SrsSettings (gRPC)
        CreateMap<SrsSettingsDto, SrsSettings>()
            .AfterMap((src, dest) =>
            {
                dest.W.Clear();
                if (src.W != null)
                {
                    dest.W.AddRange(src.W);
                }
            });

        // ProjectStats (gRPC) -> ProjectStatsDto (REST)
        CreateMap<ProjectStats, ProjectStatsDto>()
            .ForMember(dest => dest.TotalTerms, opt => opt.MapFrom(src => src.TotalLemmas))
            .ForMember(dest => dest.KnownTerms, opt => opt.MapFrom(src => src.MatureLemmas));

        // TtsSettings (gRPC) -> TtsSettingsDto (REST)
        CreateMap<TtsSettings, TtsSettingsDto>();

        // TtsSettingsDto (REST) -> TtsSettings (gRPC)
        CreateMap<TtsSettingsDto, TtsSettings>();

        // UpdateProjectDto (REST) -> UpdateProjectRequest (gRPC)
        CreateMap<UpdateProjectDto, UpdateProjectRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Устанавливается отдельно в контроллере
            .ForMember(dest => dest.ProjectId, opt => opt.Ignore()); // Устанавливается отдельно в контроллере

        // ========== Авторизация ==========

        // UserRegistrationDto (REST) -> RegisterUserRequest (gRPC)
        CreateMap<UserRegistrationDto, RegisterUserRequest>();

        // RegisterUserResponse (gRPC) -> AuthResponseDto (REST)
        CreateMap<RegisterUserResponse, AuthResponseDto>();

        // UserLoginDto (REST) -> LoginUserRequest (gRPC)
        CreateMap<UserLoginDto, LoginUserRequest>();

        // TokenResponse (gRPC) -> TokenResponseDto (REST)
        CreateMap<TokenResponse, TokenResponseDto>();

        // RefreshTokenDto (REST) -> RefreshTokenRequest (gRPC)
        CreateMap<RefreshTokenDto, RefreshTokenRequest>();

        // ConfirmEmailDto (REST) -> ConfirmEmailRequest (gRPC)
        CreateMap<ConfirmEmailDto, ConfirmEmailRequest>();

        // MessageResponse (gRPC) -> AuthResponseDto (REST)
        CreateMap<MessageResponse, AuthResponseDto>();

        // UserInfoResponse (gRPC) -> UserInfoDto (REST)
        CreateMap<UserInfoResponse, UserInfoDto>();

        // LogoutDto (REST) -> LogoutUserRequest (gRPC)
        CreateMap<LogoutDto, LogoutUserRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()); // Устанавливается отдельно в контроллере

        // UpdateUsernameDto (REST) -> UpdateUsernameRequest (gRPC)
        CreateMap<UpdateUsernameDto, UpdateUsernameRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()); // Устанавливается отдельно в контроллере

        // UpdatePasswordDto (REST) -> UpdatePasswordRequest (gRPC)
        CreateMap<UpdatePasswordDto, UpdatePasswordRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()); // Устанавливается отдельно в контроллере

        // ========== Колоды ==========

        // CreateDeckDto (REST) -> CreateDeckRequest (gRPC)
        CreateMap<CreateDeckDto, CreateDeckRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()); // Устанавливается отдельно в контроллере

        // DeckResponse (gRPC) -> DeckResponseDto (REST)
        CreateMap<DeckResponse, DeckResponseDto>()
            .ForMember(dest => dest.ContributionPolicy, opt => opt.MapFrom(src => 
                (ContributionPolicyDto)(int)src.ContributionPolicy))
            .ForMember(dest => dest.LicenseType, opt => opt.MapFrom(src => 
                (LicenseTypeDto)(int)src.LicenseType))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()));

        // DeckTreeItem (gRPC) -> DeckTreeItemDto (REST)
        // AutoMapper автоматически обработает рекурсивную структуру Children и новые поля
        CreateMap<DeckDetailStats, DeckDetailStatsDto>();
        CreateMap<DeckTreeItem, DeckTreeItemDto>()
            .ForMember(dest => dest.ForkedFromId, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.ForkedFromId) ? null : src.ForkedFromId))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.CoverImageUrl) ? null : src.CoverImageUrl));

        // DeckTreeItem (gRPC) -> DeckResponseDto (REST) - временное решение до добавления GetDeck
        // DeckTreeItem содержит только базовую информацию (Id, Title, CardCount)
        CreateMap<DeckTreeItem, DeckResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.CardCount, opt => opt.MapFrom(src => src.CardCount))
            .ForMember(dest => dest.ProjectId, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.ParentDeckId, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.OwnerId, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.Description, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.CoverImageUrl, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.IsPublic, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.ContributionPolicy, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.LicenseType, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.ForkedFromId, opt => opt.Ignore()) // Не доступно в DeckTreeItem
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()); // Не доступно в DeckTreeItem

        // UpdateDeckDto (REST) -> UpdateDeckRequest (gRPC)
        // В сгенерированном C# для google.protobuf.StringValue поля мапятся в string? —
        // оборачивание в new StringValue при присвоении string приводит к неверной сериализации/ToString() (лишние кавычки).
        CreateMap<UpdateDeckDto, UpdateDeckRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Устанавливается отдельно в контроллере
            .ForMember(dest => dest.DeckId, opt => opt.Ignore()) // Устанавливается отдельно в контроллере
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ParentDeckId, opt => opt.MapFrom(src => src.ParentDeckId))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic))
            .ForMember(dest => dest.ContributionPolicy, opt => opt.MapFrom(src =>
                src.ContributionPolicy.HasValue ? (ContributionPolicy)(int)src.ContributionPolicy.Value : (ContributionPolicy?)null));

        // ========== Настройки пользователя ==========

        // UserSettingsResponse (gRPC) -> UserSettingsResponseDto (REST)
        CreateMap<UserSettingsResponse, UserSettingsResponseDto>();

        // UpdateUserSettingsDto (REST) -> UpdateUserSettingsRequest (gRPC)
        CreateMap<UpdateUserSettingsDto, UpdateUserSettingsRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Устанавливается отдельно в контроллере
            .ForMember(dest => dest.RolloverHour, opt => opt.MapFrom(src => 
                src.RolloverHour.HasValue ? new Int32Value { Value = src.RolloverHour.Value } : null))
            .ForMember(dest => dest.DailyGoalNew, opt => opt.MapFrom(src => 
                src.DailyGoalNew.HasValue ? new Int32Value { Value = src.DailyGoalNew.Value } : null))
            .ForMember(dest => dest.DailyGoalReview, opt => opt.MapFrom(src => 
                src.DailyGoalReview.HasValue ? new Int32Value { Value = src.DailyGoalReview.Value } : null))
            .ForMember(dest => dest.InterfaceLanguage, opt => opt.MapFrom(src => 
                src.InterfaceLanguage != null ? new StringValue { Value = src.InterfaceLanguage } : null));

        // ========== Аналитика ==========

        // GetVocabularyStatsResponse (gRPC) -> VocabularyStatsResponseDto (REST)
        CreateMap<GetVocabularyStatsResponse, VocabularyStatsResponseDto>()
            .ForMember(dest => dest.TotalTerms, opt => opt.MapFrom(src => src.TotalLemmas))
            .ForMember(dest => dest.CefrLevel, opt => opt.MapFrom((src, dest, destMember, context) =>
                src.CefrLevel != null ? context.Mapper.Map<CefrLevelDto>(src.CefrLevel) : null));

        // CefrLevel (gRPC) -> CefrLevelDto (REST)
        CreateMap<CefrLevel, CefrLevelDto>();

        // GetSkillBalanceResponse (gRPC) -> SkillBalanceResponseDto (REST)
        CreateMap<GetSkillBalanceResponse, SkillBalanceResponseDto>();

        // GetHeatmapResponse (gRPC) -> HeatmapResponseDto (REST)
        CreateMap<GetHeatmapResponse, HeatmapResponseDto>()
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.ProjectId) ? src.ProjectId : null))
            .ForMember(dest => dest.TotalTimeSpentSeconds, opt => opt.MapFrom(src => src.TotalTimeSpentSeconds))
            .ForMember(dest => dest.Activity, opt => opt.MapFrom((src, dest, destMember, context) =>
                src.Activity.ToDictionary(
                    kvp => kvp.Key,
                    kvp => context.Mapper.Map<ActivityDayDto>(kvp.Value))));

        // ActivityDay (gRPC) -> ActivityDayDto (REST)
        CreateMap<ActivityDay, ActivityDayDto>();

        // GetDailySummaryResponse (gRPC) -> DailySummaryResponseDto (REST)
        CreateMap<GetDailySummaryResponse, DailySummaryResponseDto>()
            .ForMember(dest => dest.NewCards, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<GoalProgressDto>(src.NewCards)))
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<GoalProgressDto>(src.Reviews)));

        // GoalProgress (gRPC) -> GoalProgressDto (REST)
        CreateMap<GoalProgress, GoalProgressDto>();

        // ========== Карточки ==========

        CreateMap<NoteFieldValueDto, NoteFieldValuePayload>()
            .ForMember(dest => dest.StringValue, opt => opt.MapFrom(src => src.StringValue ?? string.Empty))
            .AfterMap((src, dest) =>
            {
                dest.StringValues.Clear();
                if (src.StringValues != null)
                    dest.StringValues.AddRange(src.StringValues);
            });

        CreateMap<NoteFieldValuePayload, NoteFieldValueDto>()
            .ForMember(dest => dest.StringValue, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.StringValue) ? null : src.StringValue))
            .ForMember(dest => dest.StringValues, opt => opt.MapFrom(src =>
                src.StringValues.Count == 0 ? null : src.StringValues.ToList()));

        // CreateCardDto (REST) -> CreateCardRequest (gRPC)
        CreateMap<CreateCardDto, CreateCardRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.FieldValues, opt => opt.Ignore())
            .AfterMap((src, dest, ctx) =>
            {
                dest.FieldValues.Clear();
                foreach (var kv in src.FieldValues)
                    dest.FieldValues[kv.Key] = ctx.Mapper.Map<NoteFieldValuePayload>(kv.Value);
            });

        // CaptureCardDto (REST) -> CaptureCardRequest (gRPC)
        CreateMap<CaptureCardDto, CaptureCardRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.FieldValues, opt => opt.Ignore())
            .ForMember(dest => dest.ScreenshotBase64, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.ScreenshotBase64) ? new StringValue { Value = src.ScreenshotBase64 } : null))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src =>
                !string.IsNullOrWhiteSpace(src.DeckId) ? new StringValue { Value = src.DeckId.Trim() } : null))
            .AfterMap((src, dest, ctx) =>
            {
                dest.FieldValues.Clear();
                foreach (var kv in src.FieldValues)
                    dest.FieldValues[kv.Key] = ctx.Mapper.Map<NoteFieldValuePayload>(kv.Value);
            });

        // UpdateCardDto (REST) -> UpdateCardRequest (gRPC)
        CreateMap<UpdateCardDto, UpdateCardRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CardId, opt => opt.Ignore())
            .ForMember(dest => dest.FieldValues, opt => opt.Ignore())
            .AfterMap((src, dest, ctx) =>
            {
                dest.FieldValues.Clear();
                foreach (var kv in src.FieldValues)
                    dest.FieldValues[kv.Key] = ctx.Mapper.Map<NoteFieldValuePayload>(kv.Value);
            });

        // CardResponse (gRPC) -> CardResponseDto (REST)
        CreateMap<CardResponse, CardResponseDto>()
            .ForMember(dest => dest.ProjectTermId, opt => opt.MapFrom(src =>
                src.ProjectTermId != null && !string.IsNullOrEmpty(src.ProjectTermId) ? src.ProjectTermId : null))
            .ForMember(dest => dest.SrsStatus, opt => opt.MapFrom(src => src.SrsStatus.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()))
            .ForMember(dest => dest.SrsState, opt => opt.MapFrom(src =>
                src.SrsState == null || (src.SrsState.State == SrsStatus.New && src.SrsState.CurrentInterval == 0)
                    ? null
                    : new SrsStateDto
                    {
                        State = src.SrsState.State.ToString(),
                        CurrentInterval = src.SrsState.CurrentInterval,
                        Step = src.SrsState.Step,
                        DueUtc = src.SrsState.DueUtc != null ? src.SrsState.DueUtc.ToDateTime() : null,
                        Lapses = src.SrsState.Lapses,
                        Stability = src.SrsState.Stability,
                        Difficulty = src.SrsState.Difficulty,
                        ScheduledDays = src.SrsState.ScheduledDays,
                        ElapsedDays = src.SrsState.ElapsedDays,
                    }))
            // gRPC Note / ActiveCardTemplate: no CreateMap<NotePayload, NotePayloadDto>; filled in AfterMap.
            .ForMember(dest => dest.Note, opt => opt.Ignore())
            .ForMember(dest => dest.ActiveCardTemplate, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (src.Note == null && src.ActiveCardTemplate == null) return;
                if (src.Note != null)
                {
                    dest.Note = new NotePayloadDto
                    {
                        Id = src.Note.Id,
                        NoteTypeId = src.Note.NoteTypeId,
                    };
                    // google.protobuf.StringValue maps to string in generated C# (optional string field)
                    if (!string.IsNullOrEmpty(src.Note.ProjectTermId))
                        dest.Note.ProjectTermId = src.Note.ProjectTermId;
                    foreach (var kv in src.Note.FieldValues)
                    {
                        dest.Note.FieldValues[kv.Key] = new NoteFieldValueDto
                        {
                            StringValue = string.IsNullOrEmpty(kv.Value.StringValue) ? null : kv.Value.StringValue,
                            StringValues = kv.Value.StringValues.Count > 0 ? kv.Value.StringValues.ToList() : null,
                        };
                    }
                }

                if (src.ActiveCardTemplate != null)
                {
                    dest.ActiveCardTemplate = new CardTemplateDto
                    {
                        Id = src.ActiveCardTemplate.Id,
                        TemplateKey = src.ActiveCardTemplate.TemplateKey,
                        Name = src.ActiveCardTemplate.Name,
                        FrontTemplate = src.ActiveCardTemplate.FrontTemplate,
                        BackTemplate = src.ActiveCardTemplate.BackTemplate,
                        SortOrder = src.ActiveCardTemplate.SortOrder,
                        Enabled = src.ActiveCardTemplate.Enabled,
                    };
                }
            });

        CreateMap<NoteFieldDefinitionPayload, NoteFieldDefinitionDto>();
        CreateMap<CardTemplatePayload, CardTemplateDto>();
        CreateMap<NoteTypePayload, NoteTypeForEditorDto>()
            .ForMember(dest => dest.Fields, opt => opt.MapFrom(src => src.Fields))
            .ForMember(dest => dest.Templates, opt => opt.MapFrom(src => src.Templates));
        CreateMap<GetNoteTypeForEditorResponse, GetNoteTypeForEditorResponseDto>()
            .ForMember(dest => dest.NoteType, opt => opt.MapFrom(src => src.NoteType))
            .ForMember(dest => dest.DefaultTemplate, opt => opt.MapFrom(src => src.DefaultTemplate));

        CreateMap<CheckCardDuplicatesRequestDto, CheckCardDuplicatesRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId))
            .ForMember(dest => dest.TermText, opt => opt.MapFrom(src => src.TermText));

        CreateMap<CardPreview, CardPreviewDto>()
            .ForMember(dest => dest.SrsStatus, opt => opt.MapFrom(src => src.SrsStatus.ToString()))
            .ForMember(dest => dest.HasAudio, opt => opt.MapFrom(src => (bool?)src.HasAudio))
            .ForMember(dest => dest.DeckTitle, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.DeckTitle) ? null : src.DeckTitle))
            .ForMember(dest => dest.Note, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (src.Note == null) return;
                dest.Note = new NotePayloadDto
                {
                    Id = src.Note.Id,
                    NoteTypeId = src.Note.NoteTypeId,
                };
                if (!string.IsNullOrEmpty(src.Note.ProjectTermId))
                    dest.Note.ProjectTermId = src.Note.ProjectTermId;
                foreach (var kv in src.Note.FieldValues)
                {
                    dest.Note.FieldValues[kv.Key] = new NoteFieldValueDto
                    {
                        StringValue = string.IsNullOrEmpty(kv.Value.StringValue) ? null : kv.Value.StringValue,
                        StringValues = kv.Value.StringValues.Count > 0 ? kv.Value.StringValues.ToList() : null,
                    };
                }
            });

        CreateMap<CheckCardDuplicatesResponse, CheckCardDuplicatesResponseDto>()
            .ForMember(dest => dest.NormalizedSurface, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.NormalizedSurface) ? src.NormalizedSurface : null))
            .ForMember(dest => dest.ExistingCards, opt => opt.MapFrom(src => src.ExistingCards));

        // ========== Reader: анализ текста (SR-TXT-01) ==========
        CreateMap<TextAnalyzeRequestDto, AnalyzeTextRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore());

        CreateMap<TextToken, TextTokenDto>()
            .ForMember(dest => dest.TermText, opt => opt.MapFrom(src =>
                src.TermText != null && !string.IsNullOrEmpty(src.TermText) ? src.TermText : null))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => TokenTypeToReaderApi(src.Type)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                src.Type == TokenType.Word ? TokenStatusToReaderApi(src.Status) ?? "NONE" : "NONE"));

        CreateMap<TextAnalysisStats, TextAnalysisStatsDto>();

        CreateMap<AnalyzeTextResponse, TextAnalyzeResponseDto>();

        // ========== Reader: термины (TermService gRPC) ==========
        CreateMap<CreateOrUpdateTermDto, CreateOrUpdateTermRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.Type) ? "WORD" : src.Type!.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.Language ?? string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status ?? string.Empty));

        CreateMap<TermActionDto, TermActionRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.Type) ? "WORD" : src.Type!.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.Language ?? string.Empty));

        CreateMap<BulkMarkKnownDto, BulkMarkKnownRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.Language ?? string.Empty));

        CreateMap<BulkMarkKnownResponse, BulkMarkKnownResponseDto>();

        CreateMap<SearchTermDuplicatesDto, SearchTermDuplicatesRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.Type) ? "WORD" : src.Type!.Trim().ToUpperInvariant()));

        CreateMap<TermDetailsResponse, TermDetailsDto>()
            .ForMember(dest => dest.Meaning, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.Meaning) ? null : src.Meaning))
            .ForMember(dest => dest.FirstSentence, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.FirstSentence) ? null : src.FirstSentence))
            .ForMember(dest => dest.FirstSourceTitle, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.FirstSourceTitle) ? null : src.FirstSourceTitle))
            .ForMember(dest => dest.FirstSourceUrl, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.FirstSourceUrl) ? null : src.FirstSourceUrl));

        CreateMap<SearchTermDuplicatesResponse, SearchTermDuplicatesResponseDto>();

        // SourceMetaDto (REST) -> SourceMeta (gRPC)
        CreateMap<SourceMetaDto, SourceMeta>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url))
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.Service, opt => opt.MapFrom(src => src.Service));

        // SourceMeta (gRPC) -> SourceMetaDto (REST)
        CreateMap<SourceMeta, SourceMetaDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Url) ? src.Url : null))
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src =>
                src.Page != null && src.Page.HasValue ? src.Page.Value : (int?)null))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src =>
                src.Timestamp != null && src.Timestamp.HasValue ? src.Timestamp.Value : (int?)null))
            .ForMember(dest => dest.Service, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Service) ? src.Service : null));

        // TargetIndexDto (REST) -> TargetIndex (gRPC)
        CreateMap<TargetIndexDto, TargetIndex>();

        // TargetIndex (gRPC) -> TargetIndexDto (REST)
        CreateMap<TargetIndex, TargetIndexDto>();

        // CardMediaDto (REST) -> CardMedia (gRPC)
        CreateMap<CardMediaDto, CardMedia>()
            .ForMember(dest => dest.ImageId, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.ImageId) ? new StringValue { Value = src.ImageId } : null))
            .ForMember(dest => dest.AudioId, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.AudioId) ? new StringValue { Value = src.AudioId } : null));

        // CardMedia (gRPC) -> CardMediaDto (REST)
        CreateMap<CardMedia, CardMediaDto>()
            .ForMember(dest => dest.ImageId, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.ImageId) ? src.ImageId : null))
            .ForMember(dest => dest.AudioId, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.AudioId) ? src.AudioId : null))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.ImageUrl) ? src.ImageUrl : null))
            .ForMember(dest => dest.AudioUrl, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.AudioUrl) ? src.AudioUrl : null));

        // BulkCreateCardsDto (REST) -> BulkCreateCardsRequest (gRPC)
        CreateMap<BulkCreateCardsDto, BulkCreateCardsRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Устанавливается отдельно в контроллере
            .ForMember(dest => dest.Cards, opt => opt.MapFrom((src, dest, destMember, context) =>
                src.Cards.Select(c => context.Mapper.Map<CreateCardRequest>(c)).ToList()));

        // ========== Study Service ==========

        // StartSessionRequestDto (REST) -> StartStudySessionRequest (gRPC)
        // DeckId in generated C# is string (proto optional uses ForClassWrapper), not StringValue
        CreateMap<StartSessionRequestDto, StartStudySessionRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Устанавливается отдельно в контроллере
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => src.DeckId ?? string.Empty));

        // StartStudySessionResponse (gRPC) -> StudySessionDto (REST)
        CreateMap<StartStudySessionResponse, StudySessionDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.SessionId))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.ToDateTime()))
            .ForMember(dest => dest.QueueStats, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<QueueStatsDto>(src.QueueStats)));

        // QueueStats (gRPC) -> QueueStatsDto (REST)
        CreateMap<Pvs.Content.Grpc.QueueStats, QueueStatsDto>();

        // CardStudyDto (gRPC) -> CardStudyDto (REST)
        CreateMap<Pvs.Content.Grpc.CardStudyDto, Dtos.CardStudyDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Content, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<CardStudyContentDto>(src.Content)))
            .ForMember(dest => dest.SourceMeta, opt => opt.MapFrom((src, dest, destMember, context) =>
                src.SourceMeta != null ? context.Mapper.Map<SourceMetaDto>(src.SourceMeta) : null))
            .ForMember(dest => dest.Media, opt => opt.MapFrom((src, dest, destMember, context) =>
                src.Media != null ? context.Mapper.Map<CardMediaDto>(src.Media) : null))
            .ForMember(dest => dest.SrsState, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<SrsStateDto>(src.SrsState)))
            .ForMember(dest => dest.NextIntervals, opt => opt.MapFrom(src => src.NextIntervals));

        // CardStudyContent (gRPC) -> CardStudyContentDto (REST)
        CreateMap<Pvs.Content.Grpc.CardStudyContent, CardStudyContentDto>()
            .ForMember(dest => dest.TargetIndex, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<TargetIndexDto>(src.TargetIndex)))
            .ForMember(dest => dest.Note, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (src.Note == null) return;
                dest.Note = new NotePayloadDto
                {
                    Id = src.Note.Id,
                    NoteTypeId = src.Note.NoteTypeId,
                };
                if (!string.IsNullOrEmpty(src.Note.ProjectTermId))
                    dest.Note.ProjectTermId = src.Note.ProjectTermId;
                foreach (var kv in src.Note.FieldValues)
                {
                    dest.Note.FieldValues[kv.Key] = new NoteFieldValueDto
                    {
                        StringValue = string.IsNullOrEmpty(kv.Value.StringValue) ? null : kv.Value.StringValue,
                        StringValues = kv.Value.StringValues.Count > 0 ? kv.Value.StringValues.ToList() : null,
                    };
                }
            });

        // SrsState (gRPC) -> SrsStateDto (REST)
        CreateMap<Pvs.Content.Grpc.SrsState, SrsStateDto>()
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State.ToString()))
            .ForMember(dest => dest.DueUtc, opt => opt.MapFrom(src => src.DueUtc != null ? (DateTime?)src.DueUtc.ToDateTime() : null));

        // ReviewCardRequestDto (REST) -> SubmitReviewRequest (gRPC)
        CreateMap<ReviewCardRequestDto, SubmitReviewRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Устанавливается отдельно в контроллере
            .ForMember(dest => dest.SessionId, opt => opt.Ignore()) // Устанавливается отдельно в контроллере
            .ForMember(dest => dest.CardId, opt => opt.MapFrom(src => src.CardId))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.DurationMs, opt => opt.MapFrom(src => src.DurationMs))
            .ForMember(dest => dest.UserAnswer, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.UserAnswer) 
                    ? new Google.Protobuf.WellKnownTypes.StringValue { Value = src.UserAnswer } 
                    : null));

        // SubmitReviewResponse (gRPC) -> ReviewResponseDto (REST)
        CreateMap<SubmitReviewResponse, ReviewResponseDto>()
            .ForMember(dest => dest.NextReviewDate, opt => opt.MapFrom(src => src.NextReviewDate.ToDateTime()))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State.ToString()))
            .ForMember(dest => dest.AnswerValidation, opt => opt.MapFrom((src, dest, destMember, context) =>
                src.AnswerValidation != null ? context.Mapper.Map<AnswerValidationResultDto>(src.AnswerValidation) : null));

        // AnswerValidationResult (gRPC) -> AnswerValidationResultDto (REST)
        CreateMap<Pvs.Content.Grpc.AnswerValidationResult, AnswerValidationResultDto>()
            .ForMember(dest => dest.MatchedSynonym, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.MatchedSynonym) ? src.MatchedSynonym : null))
            .ForMember(dest => dest.Suggestion, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Suggestion) ? src.Suggestion : null));

        // UndoReviewResponse (gRPC) -> UndoResponseDto (REST)
        CreateMap<UndoReviewResponse, UndoResponseDto>();

        // ========== Community Service ==========

        // Contributions

        // CreateContributionDto (REST) -> CreateContributionRequest (gRPC)
        CreateMap<Dtos.Community.CreateContributionDto, CreateContributionRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => src.DeckId.ToString()))
            .ForMember(dest => dest.CardId, opt => opt.MapFrom(src =>
                src.CardId.HasValue ? new StringValue { Value = src.CardId.Value.ToString() } : null))
            .ForMember(dest => dest.Content, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<CardContent>(src.Content)))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Comment) ? new StringValue { Value = src.Comment } : null));

        // CardContentDto (REST) -> CardContent (gRPC)
        CreateMap<Dtos.Community.CardContentDto, CardContent>()
            .ForMember(dest => dest.FieldValues, opt => opt.Ignore())
            .AfterMap((src, dest, ctx) =>
            {
                dest.FieldValues.Clear();
                foreach (var kv in src.FieldValues)
                    dest.FieldValues[kv.Key] = ctx.Mapper.Map<NoteFieldValuePayload>(kv.Value);
            });

        // ContributionDto (gRPC) -> ContributionResponseDto (REST)
        CreateMap<ContributionDto, Dtos.Community.ContributionResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
            .ForMember(dest => dest.TargetDeckId, opt => opt.MapFrom(src => Guid.Parse(src.TargetDeckId)))
            .ForMember(dest => dest.TargetCardId, opt => opt.MapFrom(src =>
                src.TargetCardId != null && !string.IsNullOrEmpty(src.TargetCardId) ? Guid.Parse(src.TargetCardId) : (Guid?)null))
            .ForMember(dest => dest.Author, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<Dtos.Community.AuthorInfoDto>(src.Author)))
            .ForMember(dest => dest.Content, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<Dtos.Community.CardContentDto>(src.Content)))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src =>
                src.Comment != null && !string.IsNullOrEmpty(src.Comment) ? src.Comment : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToDateTime()));

        // AuthorInfo (gRPC) -> AuthorInfoDto (REST)
        CreateMap<AuthorInfo, Dtos.Community.AuthorInfoDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => Guid.Parse(src.UserId)));

        // CardContent (gRPC) -> CardContentDto (REST)
        CreateMap<CardContent, Dtos.Community.CardContentDto>()
            .ForMember(dest => dest.FieldValues, opt => opt.Ignore())
            .AfterMap((src, dest, ctx) =>
            {
                dest.FieldValues.Clear();
                foreach (var kv in src.FieldValues)
                    dest.FieldValues[kv.Key] = ctx.Mapper.Map<NoteFieldValueDto>(kv.Value);
            });

        // ResolveContributionDto (REST) -> ResolveContributionRequest (gRPC)
        CreateMap<Dtos.Community.ResolveContributionDto, ResolveContributionRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.ContributionId, opt => opt.Ignore())
            .ForMember(dest => dest.ResolutionComment, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.ResolutionComment) ? new StringValue { Value = src.ResolutionComment } : null));

        // Publishing

        // PublishDeckDto (REST) -> PublishDeckRequest (gRPC)
        CreateMap<Dtos.Community.PublishDeckDto, PublishDeckRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => src.DeckId.ToString()));

        // ForkDeckDto (REST) -> ForkDeckRequest (gRPC)
        CreateMap<Dtos.Community.ForkDeckDto, ForkDeckRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => src.DeckId.ToString()))
            .ForMember(dest => dest.TargetProjectId, opt => opt.MapFrom(src => src.TargetProjectId.ToString()))
            .ForMember(dest => dest.NewTitle, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.NewTitle) ? new StringValue { Value = src.NewTitle } : null));

        // PublishedDeckDto (gRPC) -> PublishedDeckResponseDto (REST)
        CreateMap<PublishedDeckDto, Dtos.Community.PublishedDeckResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.LicenseType, opt => opt.MapFrom(src => src.LicenseType))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Description) ? src.Description : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.CoverImageUrl) ? src.CoverImageUrl : null))
            .ForMember(dest => dest.Author, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<Dtos.Community.AuthorInfoDto>(src.Author)))
            .ForMember(dest => dest.CardCount, opt => opt.MapFrom(src => src.CardCount))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => (double?)null))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.ForkCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.PublishedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToDateTime()));

        // GetAuthorProfileResponse (gRPC) -> AuthorProfileDto (REST)
        CreateMap<GetAuthorProfileResponse, Dtos.Community.AuthorProfileDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => Guid.Parse(src.AuthorId)))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
            .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => (string?)null))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => (string?)null))
            .ForMember(dest => dest.PublishedDecksCount, opt => opt.MapFrom(src => src.PublishedDecksCount))
            .ForMember(dest => dest.TotalForksCount, opt => opt.MapFrom(src => src.TotalSales))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => (double?)src.AverageRating))
            .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => DateTime.UtcNow)); // Not available in response

        // Marketplace

        // CreateProductDto (REST) -> CreateProductRequest (gRPC)
        CreateMap<Dtos.Community.CreateProductDto, CreateProductRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => src.DeckId.ToString()))
            .ForMember(dest => dest.DescriptionHtml, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.DescriptionHtml) ? new StringValue { Value = src.DescriptionHtml } : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.CoverImageUrl) ? new StringValue { Value = src.CoverImageUrl } : null));

        // UpdateProductDto (REST) -> UpdateProductRequest (gRPC)
        CreateMap<Dtos.Community.UpdateProductDto, UpdateProductRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Title) ? new StringValue { Value = src.Title } : null))
            .ForMember(dest => dest.DescriptionHtml, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.DescriptionHtml) ? new StringValue { Value = src.DescriptionHtml } : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.CoverImageUrl) ? new StringValue { Value = src.CoverImageUrl } : null))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src =>
                src.Price.HasValue ? new Google.Protobuf.WellKnownTypes.DoubleValue { Value = (double)src.Price.Value } : null))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Currency) ? new StringValue { Value = src.Currency } : null));

        // ProductDto (gRPC) -> ProductResponseDto (REST)
        CreateMap<ProductDto, Dtos.Community.ProductResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
            .ForMember(dest => dest.DeckId, opt => opt.MapFrom(src => Guid.Parse(src.DeckId)))
            .ForMember(dest => dest.Author, opt => opt.MapFrom((src, dest, destMember, context) =>
                context.Mapper.Map<Dtos.Community.AuthorInfoDto>(src.Author)))
            .ForMember(dest => dest.DescriptionHtml, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.DescriptionHtml) ? src.DescriptionHtml : null))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.CoverImageUrl) ? src.CoverImageUrl : null))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => (double?)src.AverageRating))
            .ForMember(dest => dest.PurchaseCount, opt => opt.MapFrom(src => src.SalesCount))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToDateTime()));

        // CreateReviewDto (REST) -> CreateReviewRequest (gRPC)
        CreateMap<Dtos.Community.CreateReviewDto, CreateReviewRequest>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId.ToString()))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Comment) ? new StringValue { Value = src.Comment } : null));

        // GetProductStatsResponse (gRPC) -> ProductStatsDto (REST)
        CreateMap<GetProductStatsResponse, Dtos.Community.ProductStatsDto>()
            .ForMember(dest => dest.TotalPurchases, opt => opt.MapFrom(src => src.SalesCount))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.AverageRating))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.ReviewsCount))
            .ForMember(dest => dest.RefundCount, opt => opt.MapFrom(src => 0)) // Not available in response
            .ForMember(dest => dest.TotalRevenue, opt => opt.MapFrom(src => 0.0m)); // Not available in response

        // CheckEntitlementResponse (gRPC) -> EntitlementDto (REST)
        CreateMap<CheckEntitlementResponse, Dtos.Community.EntitlementDto>()
            .ForMember(dest => dest.AccessType, opt => opt.MapFrom(src => src.Source))
            .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => (DateTime?)null)); // Not available in response

        // ========== Agent ==========

        CreateMap<AgentThreadListItem, Dtos.Agent.AgentThreadListItemDto>()
            .ForMember(dest => dest.AgentId, opt => opt.MapFrom(src => src.AgentId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToDateTime()));

        CreateMap<AgentThreadResponse, Dtos.Agent.AgentThreadDto>()
            .ForMember(dest => dest.AgentId, opt => opt.MapFrom(src => src.AgentId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToDateTime()))
            .ForMember(dest => dest.ArchivedAt, opt => opt.MapFrom(src =>
                src.ArchivedAt != null && src.ArchivedAt.Seconds > 0
                    ? (DateTime?)src.ArchivedAt.ToDateTime()
                    : null));

        CreateMap<AgentMessageItem, Dtos.Agent.AgentMessageDto>()
            .ForMember(dest => dest.MetadataJson, opt => opt.MapFrom(src =>
                src.MetadataJson != null && !string.IsNullOrEmpty(src.MetadataJson) ? src.MetadataJson : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToDateTime()));

        CreateMap<AgentRunItem, Dtos.Agent.AgentRunDto>()
            .ForMember(dest => dest.Model, opt => opt.MapFrom(src =>
                src.Model != null && !string.IsNullOrEmpty(src.Model) ? src.Model : null))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => src.StartedAt.ToDateTime()))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src =>
                src.CompletedAt != null && src.CompletedAt.Seconds > 0
                    ? (DateTime?)src.CompletedAt.ToDateTime()
                    : null));

        CreateMap<Dtos.Agent.CreateAgentRunRequestDto, CreateAgentRunRequest>()
            .ForMember(dest => dest.UserMessage, opt => opt.MapFrom(src => src.UserMessage))
            .ForMember(dest => dest.AssistantMessage, opt => opt.MapFrom(src => src.AssistantMessage))
            .ForMember(dest => dest.DomainDecision, opt => opt.MapFrom(src => src.DomainDecision))
            .ForMember(dest => dest.ToolCalls, opt => opt.MapFrom(src => src.ToolCalls))
            .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Model ?? string.Empty));

        CreateMap<Dtos.Agent.AgentMessageInputDto, AgentMessageInput>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? string.Empty))
            .ForMember(dest => dest.MetadataJson, opt => opt.MapFrom(src => src.MetadataJson ?? string.Empty));

        CreateMap<Dtos.Agent.AgentDomainDecisionDto, AgentDomainDecisionInput>()
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason ?? string.Empty));

        CreateMap<Dtos.Agent.AgentToolCallDto, AgentToolCallInput>();
    }

    /// <summary>Статус токена для JSON reader (совпадает с TextTokenStatus на фронте).</summary>
    private static string? TokenStatusToReaderApi(TokenStatus s) => s switch
    {
        TokenStatus.New => "NEW",
        TokenStatus.Learning => "LEARNING",
        TokenStatus.Known => "KNOWN",
        TokenStatus.Ignored => "IGNORED",
        _ => "NEW",
    };

    private static string TokenTypeToReaderApi(TokenType t) => t switch
    {
        TokenType.Word => "WORD",
        TokenType.Space => "SPACE",
        TokenType.Punctuation => "PUNCTUATION",
        _ => "WORD",
    };
}
