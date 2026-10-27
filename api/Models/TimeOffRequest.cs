
namespace ReistersDaughter.Api.Models;

public enum TimeOffRequestStatus
{
    Pending,
    Approved,
    Denied,
    Cancelled
}

public class TimeOffRequest
{
    public int Id { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string? Reason { get; set; }

    public TimeOffRequestStatus Status { get; set; }
        = TimeOffRequestStatus.Pending;

    public int? ReviewedByAppUserId { get; set; }
    public AppUser? ReviewedBy { get; set; }

    public string? ManagerNote { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAt { get; set; }
}
