namespace ReistersDaughter.Api.Models;

public class Communication
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? InquiryId { get; set; }
    public Inquiry? Inquiry { get; set; }

    public string Type { get; set; } = "Email";
    public string Direction { get; set; } = "Outbound";

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;

    public string Status { get; set; } = "Draft";

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? SentAt { get; set; }
}