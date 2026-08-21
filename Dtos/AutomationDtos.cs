using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos;

public class DailyAutopilotDto
{
    public string UserId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string? DeckId { get; set; }
    public string PlanDate { get; set; } = string.Empty;
    public int SuggestedMinutes { get; set; }
    public int SuggestedNewCards { get; set; }
    public int SuggestedReviews { get; set; }
    public int BacklogRiskScore { get; set; }
    public string SessionMode { get; set; } = "AUTOPILOT";
    public List<NextBestActionDto> NextBestActions { get; set; } = [];
}

public class NextBestActionDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? DeckId { get; set; }
}

public class NotificationPreferencesDto
{
    public bool EnableStudyReminders { get; set; } = true;
    public bool EnableStreakRiskAlerts { get; set; } = true;
    public bool EnableBacklogAlerts { get; set; } = true;
    public bool EnableContributionEvents { get; set; } = true;
    public bool EnableMarketplaceEvents { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = false;
    public bool InAppEnabled { get; set; } = true;
    [Range(0, 23)]
    public int QuietHoursStart { get; set; } = 22;
    [Range(0, 23)]
    public int QuietHoursEnd { get; set; } = 8;
}

public class UpdateNotificationPreferencesDto
{
    public bool? EnableStudyReminders { get; set; }
    public bool? EnableStreakRiskAlerts { get; set; }
    public bool? EnableBacklogAlerts { get; set; }
    public bool? EnableContributionEvents { get; set; }
    public bool? EnableMarketplaceEvents { get; set; }
    public bool? PushEnabled { get; set; }
    public bool? EmailEnabled { get; set; }
    public bool? InAppEnabled { get; set; }
    [Range(0, 23)]
    public int? QuietHoursStart { get; set; }
    [Range(0, 23)]
    public int? QuietHoursEnd { get; set; }
}

/// <summary>Состояние джоба автоматизации (импорт и т.д.) для polling UI.</summary>
public class AutomationJobDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<string> Logs { get; set; } = [];
    public Dictionary<string, object>? Result { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
}

/// <summary>Тело POST /api/automation/jobs (например type=IMPORT).</summary>
public class CreateAutomationJobDto
{
    [Required]
    public string Type { get; set; } = string.Empty;
    public string? ProjectId { get; set; }
    public string? DeckId { get; set; }
    public int? ItemsCount { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
}

public class MiningDraftCardDto
{
    public string DraftId { get; set; } = string.Empty;
    public string Sentence { get; set; } = string.Empty;
    public string TargetWord { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string TermText { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class ZeroTouchMiningRequestDto
{
    [Required]
    public string ProjectId { get; set; } = string.Empty;
    [Required]
    public string SourceText { get; set; } = string.Empty;
    public string? SourceTitle { get; set; }
}

public class ZeroTouchMiningResponseDto
{
    public string ProjectId { get; set; } = string.Empty;
    public int TotalDrafts { get; set; }
    public List<MiningDraftCardDto> Drafts { get; set; } = [];
}

public class ApproveMiningDraftsRequestDto
{
    [Required]
    public string DeckId { get; set; } = string.Empty;
    public List<MiningDraftCardDto> Drafts { get; set; } = [];
}

public class CopilotReviewFeedbackRequestDto
{
    [Required]
    public string CardId { get; set; } = string.Empty;
    [Required]
    public string Sentence { get; set; } = string.Empty;
    [Required]
    public string TargetWord { get; set; } = string.Empty;
    [Required]
    public string Translation { get; set; } = string.Empty;
    public string? UserAnswer { get; set; }
    [Range(1, 4)]
    public int Rating { get; set; }
}

public class CopilotRemedialCardDto
{
    public string Sentence { get; set; } = string.Empty;
    public string TargetWord { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
}

public class CopilotReviewFeedbackDto
{
    public string Tone { get; set; } = "neutral";
    public string Explanation { get; set; } = string.Empty;
    public string ActionHint { get; set; } = string.Empty;
    public bool SuggestRemedialCards { get; set; }
    public List<CopilotRemedialCardDto> RemedialCards { get; set; } = [];
}

public class ExperimentAssignmentDto
{
    public string Key { get; set; } = string.Empty;
    public string Variant { get; set; } = "control";
}

public class TrackExperimentEventDto
{
    [Required]
    public string Key { get; set; } = string.Empty;
    [Required]
    public string Variant { get; set; } = string.Empty;
    [Required]
    public string EventName { get; set; } = string.Empty;
    public string? ProjectId { get; set; }
    public string? DeckId { get; set; }
}
