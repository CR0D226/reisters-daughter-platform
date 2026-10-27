namespace ReistersDaughter.Api.Models;

public class AvailabilityEntry
{
    public int Id { get; set; }

    public int AvailabilityPeriodId { get; set; }
    public AvailabilityPeriod AvailabilityPeriod { get; set; }
        = null!;

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}