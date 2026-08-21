using AggregatorService.Dtos;

namespace AggregatorService.Services;

/// <summary>Хранилище и планировщик фоновых джобов автоматизации.</summary>
public interface IAutomationJobOrchestrator
{
    AutomationJobDto CreateJob(string type, Dictionary<string, object>? payload);
    AutomationJobDto? GetJob(string id);
    void EnqueueRun(string id);
}
