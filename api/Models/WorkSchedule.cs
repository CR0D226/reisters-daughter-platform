namespace ReistersDaughter.Api.Models;

public enum WorkScheduleStatus
{
    Draft,
    Published
}

public class WorkSchedule
{
    public int Id { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public WorkScheduleStatus Status { get; set; }
        = WorkScheduleStatus.Draft;

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}