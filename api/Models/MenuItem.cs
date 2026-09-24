namespace ReistersDaughter.Api.Models;

public class MenuItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool PriceVaries { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Subcategory { get; set; } = string.Empty;

    public bool Vegan { get; set; }

    public bool GlutenFree { get; set; }

    public bool Featured { get; set; }

    public string? ImageUrl { get; set; }
}