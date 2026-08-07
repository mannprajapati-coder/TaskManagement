using TaskPlatform.Shared.ViewModels.TimeTracking;

namespace Modules.TimeTracking.Domain.IServices
{
    public interface ITimeTrackingService
    {
        Task<TimeLogViewModel> StartTimerAsync(Guid userId, Guid taskId);
        Task<TimeLogViewModel> PauseTimerAsync(Guid userId);
        Task<TimeLogViewModel> ResumeTimerAsync(Guid userId);
        Task<TimeLogViewModel> StopTimerAsync(Guid userId, string? notes);
        Task<ActiveTimerViewModel?> GetActiveTimerAsync(Guid userId);
        Task<List<TimeLogViewModel>> GetTaskTimeLogsAsync(Guid taskId);
        Task<TimeLogViewModel> UpdateNotesAsync(Guid userId, Guid timeLogId, string? notes);
        Task<TimeTrackingReportViewModel> GetReportAsync(Guid userId, Guid workspaceId, DateTime from, DateTime to);
    }
}
