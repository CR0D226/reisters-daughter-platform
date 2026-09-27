namespace ReistersDaughter.Api.Models;

public class Quote
{
    public int Id { get; set; }

    public int InquiryId { get; set; }
    public Inquiry Inquiry { get; set; } = null!;

    public string QuoteNumber { get; set; } = string.Empty;

    public string Status { get; set; } = "Draft";

    public string? PublicToken { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? CustomerMessage { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public List<QuoteItem> Items { get; set; } = [];
    
}