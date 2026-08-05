namespace TaskPlatform.Shared.Helpers
{
    public static class TaskStatusDisplay
    {
        public static readonly string[] AllStatuses = { "Todo", "InProgress", "InReview", "Completed", "Cancelled" };

        public static string GetLabel(string status) => status switch
        {
            "InProgress" => "In Progress",
            "InReview" => "In Review",
            _ => status
        };

        public static string GetBadgeClass(string status) => status switch
        {
            "Completed" => "bg-success-subtle text-success border border-success",
            "InProgress" => "bg-primary-subtle text-primary border border-primary",
            "InReview" => "bg-warning-subtle text-dark border border-warning",
            "Cancelled" => "bg-secondary-subtle text-secondary border border-secondary",
            _ => "bg-light text-dark border"
        };

        public static string GetPriorityBadgeClass(string priority) => priority switch
        {
            "Urgent" => "bg-danger text-white",
            "High" => "bg-warning-subtle text-dark border border-warning",
            "Medium" => "bg-info-subtle text-info border border-info",
            _ => "bg-secondary-subtle text-secondary"
        };
    }
}
