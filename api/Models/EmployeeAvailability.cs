namespace ReistersDaughter.Api.Models;

public class EmployeeAvailability
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; } = null!;

    // 0 = Sunday, 1 = Monday, ... 6 = Saturday
    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}