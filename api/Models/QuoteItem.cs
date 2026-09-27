namespace ReistersDaughter.Api.Models;

public class QuoteItem
{
    public int Id { get; set; }

    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    // Quote quantities are whole units:
    // 1 delivery, 2 coffee travelers, 24 croissants, etc.
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int SortOrder { get; set; }
}