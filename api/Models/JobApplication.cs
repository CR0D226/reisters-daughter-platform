namespace ReistersDaughter.Api.Models;

public class JobApplication
{
    public int Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public string Status { get; set; } = "New";

    public string Position { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public bool AvailableEarlyMornings { get; set; }
    public bool AvailableWeekends { get; set; }

    public string DesiredWeeklyHours { get; set; } = string.Empty;
    public DateOnly? AvailableStartDate { get; set; }

    public string Experience { get; set; } = string.Empty;
    public bool? HasFoodSafetyCertification { get; set; }
    public bool? HasReliableTransportation { get; set; }

    public string Introduction { get; set; } = string.Empty;

    public string? ResumeStorageKey { get; set; }
    public string? ResumeOriginalFileName { get; set; }

    public List<JobApplicationNote> Notes { get; set; } = [];
    public List<JobApplicationActivity> Activities { get; set; } = [];
}

public class JobApplicationNote
{
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}

public class JobApplicationActivity
{
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}
