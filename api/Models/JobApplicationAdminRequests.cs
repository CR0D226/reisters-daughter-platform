namespace ReistersDaughter.Api.Models;

public sealed class JobApplicationStatusRequest
{
    public string? Status { get; set; }
}

public sealed class JobApplicationNoteRequest
{
    public string? Body { get; set; }
}
