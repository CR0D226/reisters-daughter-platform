namespace ReistersDaughter.Api.Models;

public class EmployeePosition
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; } = null!;

    public string Position { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}
