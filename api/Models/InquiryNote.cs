namespace ReistersDaughter.Api.Models;

public class InquiryNote
{
    public int Id { get; set; }

    public int InquiryId { get; set; }
    public Inquiry Inquiry { get; set; } = null!;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}