namespace ReistersDaughter.Api.Models;

public class Event
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Examples: High Tea, Class, Community Event, Private Event, Catering
    public string Type { get; set; } = string.Empty;

    // Draft, Published, Completed, Cancelled
    public string Status { get; set; } = "Draft";

    public string StartDate { get; set; } = string.Empty;

    public string StartTime { get; set; } = string.Empty;

    public string? EndDate { get; set; }

    public string? EndTime { get; set; }

    public string? Location { get; set; }

    // Controls whether this event can eventually appear on the public website.
    public bool IsPublic { get; set; }

    // Optional because some events do not have limited seating.
    public int? Capacity { get; set; }

    // An event can optionally originate from an existing private booking.
    public int? BookingId { get; set; }

    public Booking? Booking { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}