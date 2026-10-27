namespace ReistersDaughter.Api.Models;

public sealed class JobApplicationRequest
{
    public string? Position { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public bool AvailableEarlyMornings { get; set; }
    public bool AvailableWeekends { get; set; }

    public string? DesiredWeeklyHours { get; set; }
    public DateOnly? AvailableStartDate { get; set; }

    public string? Experience { get; set; }
    public bool? HasFoodSafetyCertification { get; set; }
    public bool? HasReliableTransportation { get; set; }

    public string? Introduction { get; set; }
}
