namespace ReistersDaughter.Api.Models;

public enum AvailabilitySubmissionStatus
{
    Submitted,
    CarriedForward,
    ManagerOverride
}

public class AvailabilitySubmission
{
    public int Id { get; set; }

    public int AvailabilityPeriodId { get; set; }
    public AvailabilityPeriod AvailabilityPeriod { get; set; }
        = null!;

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public AvailabilitySubmissionStatus Status { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}