namespace ReistersDaughter.Api.Models;

public class ShiftAssignment
{
    public int Id { get; set; }

    public int WorkShiftId { get; set; }
    public WorkShift WorkShift { get; set; }
        = null!;

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; }
        = null!;
}