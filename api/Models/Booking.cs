namespace ReistersDaughter.Api.Models;

public class Booking
{
    public int Id { get; set; }

    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public int InquiryId { get; set; }
    public Inquiry Inquiry { get; set; } = null!;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Status { get; set; } = "Confirmed";

    public string EventType { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
    public string EventTime { get; set; } = string.Empty;

    public int GuestCount { get; set; }

    public decimal Total { get; set; }

    public string? InternalNotes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}