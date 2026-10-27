namespace ReistersDaughter.Api.Models;

public class WorkShift
{
    public int Id { get; set; }

    public int WorkScheduleId { get; set; }
    public WorkSchedule WorkSchedule { get; set; }
        = null!;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public string Position { get; set; } = string.Empty;

    public int RequiredEmployees { get; set; } = 1;
}