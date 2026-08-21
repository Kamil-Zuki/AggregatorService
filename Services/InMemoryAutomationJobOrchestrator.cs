using System.Collections.Concurrent;
using AggregatorService.Dtos;

namespace AggregatorService.Services;

public sealed class InMemoryAutomationJobOrchestrator : IAutomationJobOrchestrator
{
    private readonly ConcurrentDictionary<string, AutomationJobDto> _jobs = new();
    private readonly ILogger<InMemoryAutomationJobOrchestrator> _logger;

    public InMemoryAutomationJobOrchestrator(ILogger<InMemoryAutomationJobOrchestrator> logger)
    {
        _logger = logger;
    }

    public AutomationJobDto CreateJob(string type, Dictionary<string, object>? payload)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var job = new AutomationJobDto
        {
            Id = id,
            Type = type,
            Status = "QUEUED",
            ProgressPercent = 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Logs = [$"[{DateTime.UtcNow:HH:mm:ss}] Job '{type}' queued."],
            Payload = payload
        };
        _jobs[id] = job;
        _logger.LogInformation("Created automation job {JobId} of type {JobType}", id, type);
        return job;
    }

    public AutomationJobDto? GetJob(string id) => _jobs.TryGetValue(id, out var job) ? job : null;

    public void EnqueueRun(string id)
    {
        _ = Task.Run(async () => await ExecuteAsync(id));
    }

    private async Task ExecuteAsync(string id)
    {
        if (!_jobs.TryGetValue(id, out var job))
            return;

        try
        {
            UpdateJob(job, status: "RUNNING", progress: 5, log: "Starting execution...");
            await Task.Delay(600);

            switch (job.Type.ToLowerInvariant())
            {
                case "card-janitor":
                    await RunCardJanitorAsync(job);
                    break;
                case "deep-miner":
                    await RunDeepMinerAsync(job);
                    break;
                default:
                    UpdateJob(job, status: "FAILED", progress: 100, log: $"Unknown job type '{job.Type}'.");
                    job.LastError = $"Unknown job type '{job.Type}'.";
                    return;
            }

            UpdateJob(job, status: "COMPLETED", progress: 100, log: "Execution completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automation job {JobId} failed", id);
            UpdateJob(job, status: "FAILED", progress: job.ProgressPercent, log: $"Failed: {ex.Message}");
            job.LastError = ex.Message;
        }
    }

    private async Task RunCardJanitorAsync(AutomationJobDto job)
    {
        var threshold = GetPayloadInt(job, "threshold", 8);
        var includeMissingMedia = GetPayloadBool(job, "includeMissingMedia", true);

        UpdateJob(job, progress: 15, log: "Scanning leech cards (high lapse count)...");
        await Task.Delay(800);
        var leechCount = Random.Shared.Next(2, 12);

        UpdateJob(job, progress: 35, log: $"Found {leechCount} leech card(s) with lapses >= {threshold}.");
        await Task.Delay(600);

        if (includeMissingMedia)
        {
            UpdateJob(job, progress: 55, log: "Scanning cards missing audio or image...");
            await Task.Delay(800);
            var missingMediaCount = Random.Shared.Next(3, 20);
            UpdateJob(job, progress: 70, log: $"Found {missingMediaCount} card(s) missing media.");
            await Task.Delay(600);
        }

        UpdateJob(job, progress: 85, log: "Detecting duplicates and empty notes...");
        await Task.Delay(800);
        var duplicateCount = Random.Shared.Next(0, 6);
        var emptyNoteCount = Random.Shared.Next(0, 9);
        UpdateJob(job, progress: 95, log: $"Found {duplicateCount} duplicate(s), {emptyNoteCount} empty note(s).");

        job.Result = new Dictionary<string, object>
        {
            ["leechCount"] = leechCount,
            ["missingMediaCount"] = includeMissingMedia ? Random.Shared.Next(3, 20) : 0,
            ["duplicateCount"] = duplicateCount,
            ["emptyNoteCount"] = emptyNoteCount,
            ["suggestedAction"] = "Review leeches and regenerate missing media."
        };
    }

    private async Task RunDeepMinerAsync(AutomationJobDto job)
    {
        var source = GetPayloadString(job, "source", "project texts");

        UpdateJob(job, progress: 15, log: $"Reading source: {source}...");
        await Task.Delay(800);

        UpdateJob(job, progress: 35, log: "Tokenizing and lemmatizing content...");
        await Task.Delay(900);

        UpdateJob(job, progress: 55, log: "Selecting high-value unknown terms...");
        await Task.Delay(800);
        var minedTerms = Random.Shared.Next(5, 25);
        UpdateJob(job, progress: 75, log: $"Selected {minedTerms} candidate term(s).");

        UpdateJob(job, progress: 90, log: "Generating example sentences and translations...");
        await Task.Delay(900);

        var draftsCount = Random.Shared.Next(Math.Max(1, minedTerms - 5), minedTerms);
        UpdateJob(job, progress: 98, log: $"Produced {draftsCount} draft card(s) ready for review.");

        job.Result = new Dictionary<string, object>
        {
            ["minedTerms"] = minedTerms,
            ["draftsCount"] = draftsCount,
            ["source"] = source,
            ["suggestedAction"] = "Approve drafts in the card browser."
        };
    }

    private static void UpdateJob(AutomationJobDto job, string? status = null, int? progress = null, string? log = null)
    {
        if (status is not null)
            job.Status = status;
        if (progress is not null)
            job.ProgressPercent = progress.Value;
        if (log is not null)
            job.Logs.Add($"[{DateTime.UtcNow:HH:mm:ss}] {log}");
        job.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static int GetPayloadInt(AutomationJobDto job, string key, int defaultValue)
    {
        if (job.Payload is not null && job.Payload.TryGetValue(key, out var value)) return Convert.ToInt32(value);
        return defaultValue;
    }

    private static bool GetPayloadBool(AutomationJobDto job, string key, bool defaultValue)
    {
        if (job.Payload is not null && job.Payload.TryGetValue(key, out var value)) return Convert.ToBoolean(value);
        return defaultValue;
    }

    private static string GetPayloadString(AutomationJobDto job, string key, string defaultValue)
    {
        if (job.Payload is not null && job.Payload.TryGetValue(key, out var value)) return value?.ToString() ?? defaultValue;
        return defaultValue;
    }
}
