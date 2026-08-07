namespace Modules.TimeTracking.Domain.Entities
{
    public class TimeLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Running"; // Running | Paused | Stopped
        public int AccumulatedSeconds { get; set; }
        public DateTime? LastResumedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
