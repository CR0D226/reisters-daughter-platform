namespace ReistersDaughter.Api.Models;

public class InquiryActivity
{
    public int Id { get; set; }

    public int InquiryId { get; set; }
    public Inquiry Inquiry { get; set; } = null!;

    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
