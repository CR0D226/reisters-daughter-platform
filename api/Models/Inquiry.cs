namespace ReistersDaughter.Api.Models;

public class Inquiry
{
    // Database fields
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "New";


    // Inquiry type
    public string EventType { get; set; } = string.Empty;

    public List<string> Services { get; set; } = [];


    // Contact information
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Company { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;


    // Event information
    public string EventDate { get; set; } = string.Empty;

    public string EventTime { get; set; } = string.Empty;

    public int GuestCount { get; set; }


    // Catering information
    public string? CateringType { get; set; }

    public string? ServiceType { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? Packaging { get; set; }

    public List<string> DietaryNeeds { get; set; } = [];

    public string? OtherDietaryNeeds { get; set; }

    public string? Recurring { get; set; }


    // Additional information
    public string Details { get; set; } = string.Empty;
}