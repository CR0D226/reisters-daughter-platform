namespace ReistersDaughter.Api.Models;

public class AvailabilityPeriod
{
    public int Id { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public DateTimeOffset Deadline { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}