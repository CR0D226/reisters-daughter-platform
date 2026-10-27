using Microsoft.EntityFrameworkCore;
using ReistersDaughter.Api.Data;
using ReistersDaughter.Api.Models;

namespace ReistersDaughter.Api.Services;

public class AvailabilityRolloverService
{
    private readonly AppDbContext _db;

    public AvailabilityRolloverService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string[]> PreviewAsync(
        DateOnly startDate,
        CancellationToken cancellationToken = default)
    {
        var firstStart = new DateOnly(2026, 11, 2);
        var offset = startDate.DayNumber - firstStart.DayNumber;

        if (offset < 0 || offset % 14 != 0)
            throw new ArgumentException("Invalid scheduling period.");

        var eastern = TimeZoneInfo.FindSystemTimeZoneById(
            "America/New_York");

        var deadlineDate = startDate.AddDays(-18);
        var localDeadline = deadlineDate.ToDateTime(
            TimeOnly.MinValue).AddDays(1);

        var deadline = new DateTimeOffset(
            localDeadline,
            eastern.GetUtcOffset(localDeadline)
        ).ToUniversalTime();

        if (DateTimeOffset.UtcNow < deadline)
            return Array.Empty<string>();

        var period = await _db.AvailabilityPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.StartDate == startDate,
                cancellationToken);

        var employees = await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == "Employee")
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        var submittedIds = period is null
            ? new HashSet<int>()
            : (await _db.AvailabilitySubmissions
                .AsNoTracking()
                .Where(s => s.AvailabilityPeriodId == period.Id)
                .Select(s => s.AppUserId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        var previousPeriod = await _db.AvailabilityPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.StartDate == startDate.AddDays(-14),
                cancellationToken);

        var previousSubmissionIds = previousPeriod is null
            ? new HashSet<int>()
            : (await _db.AvailabilitySubmissions
                .AsNoTracking()
                .Where(s => s.AvailabilityPeriodId == previousPeriod.Id)
                .Select(s => s.AppUserId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        var results = new List<string>();

        foreach (var employee in employees)
        {
            if (submittedIds.Contains(employee.Id))
                continue;

            var entries = new List<(DateOnly Date,
                TimeOnly Start, TimeOnly End)>();

            string source;

            if (previousSubmissionIds.Contains(employee.Id))
            {
                source = "PreviousPeriod";

                var previousEntries = await _db.AvailabilityEntries
                    .AsNoTracking()
                    .Where(e =>
                        e.AvailabilityPeriodId == previousPeriod!.Id &&
                        e.AppUserId == employee.Id)
                    .ToListAsync(cancellationToken);

                entries.AddRange(previousEntries.Select(e => (
                    e.Date.AddDays(14),
                    e.StartTime,
                    e.EndTime
                )));
            }
            else
            {
                source = "WeeklyTemplate";

                var weekly = await _db.EmployeeAvailabilities
                    .AsNoTracking()
                    .Where(a => a.AppUserId == employee.Id)
                    .ToListAsync(cancellationToken);

                for (var i = 0; i < 14; i++)
                {
                    var date = startDate.AddDays(i);

                    entries.AddRange(
                        weekly
                            .Where(w => w.DayOfWeek == date.DayOfWeek)
                            .Select(w => (
                                date,
                                w.StartTime,
                                w.EndTime
                            )));
                }
            }

            var ordered = entries
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Start)
                .ToList();

            var valid = ordered.All(e =>
                e.Date >= startDate &&
                e.Date <= startDate.AddDays(13) &&
                e.End > e.Start);

            var overlaps = ordered
                .GroupBy(e => e.Date)
                .Any(group =>
                {
                    var windows = group
                        .OrderBy(e => e.Start)
                        .ToList();

                    return windows
                        .Skip(1)
                        .Where((entry, index) =>
                            entry.Start < windows[index].End)
                        .Any();
                });

            if (!valid || overlaps)
            {
                results.Add(
                    $"Employee {employee.Id}: INVALID availability");
                continue;
            }

            results.Add(
                $"Employee {employee.Id}: {source}, " +
                $"{ordered.Count} entries");

            results.AddRange(ordered.Select(e =>
                $"  {e.Date:yyyy-MM-dd} {e.Start:HH:mm}-{e.End:HH:mm}"));
        }

        return results.ToArray();
    }
}
