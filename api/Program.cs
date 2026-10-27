using Microsoft.EntityFrameworkCore;
using ReistersDaughter.Api.Data;
using ReistersDaughter.Api.Models;
using ReistersDaughter.Api.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;



var builder = WebApplication.CreateBuilder(args);

// ResumeUploadSecurityV1
// Limit multipart request bodies before form parsing.
const long JobUploadRequestLimit = 6L * 1024 * 1024;

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(
    options =>
    {
        options.MultipartBodyLengthLimit = JobUploadRequestLimit;
        options.ValueLengthLimit = 64 * 1024;
        options.ValueCountLimit = 10;
        options.MultipartHeadersLengthLimit = 8 * 1024;
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("JobApplicationSubmission", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});


// Private résumé storage — outside the web root.
var resumeDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".reisters-daughter",
    "private",
    "resumes");

builder.Configuration["ResumeStorage:Directory"] =
    builder.Configuration["ResumeStorage:Directory"]
    ?? resumeDirectory;
builder.Services.AddSingleton<ResumeStorageService>();


// =========================================================
// KEYCLOAK JWT AUTHENTICATION
// =========================================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
    // Preserve Keycloak's original JWT claim names.
    options.MapInboundClaims = false;

    // RAILWAY PRODUCTION CONFIG V1
    options.Authority = builder.Configuration["Keycloak:Authority"]
        ?? "https://192.168.0.176:8443/realms/reisters-daughter";

    options.RequireHttpsMetadata =
        !builder.Environment.IsDevelopment();

    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidateAudience = true,
            ValidAudience = "reisters-api"
        };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveUser", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var db = context.Resource is HttpContext httpContext
                ? httpContext.RequestServices
                    .GetRequiredService<AppDbContext>()
                : null;

            var subject = context.User.FindFirst("sub")?.Value;

            if (db is null || string.IsNullOrWhiteSpace(subject))
                return false;

            return db.AppUsers.Any(user =>
                user.ExternalId == subject &&
                user.IsActive);
        });
    });

    options.AddPolicy("ManagerOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var db = context.Resource is HttpContext httpContext
                ? httpContext.RequestServices
                    .GetRequiredService<AppDbContext>()
                : null;

            var subject = context.User.FindFirst("sub")?.Value;

            if (db is null || string.IsNullOrWhiteSpace(subject))
                return false;

            return db.AppUsers.Any(user =>
                user.ExternalId == subject &&
                user.IsActive &&
                user.Role == "Manager");
        });
    });
});


// =========================================================
// DATABASE
// =========================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);


// =========================================================
// CORS
// =========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "https://127.0.0.1:5173",
                "http://192.168.0.176:5173",
                "https://192.168.0.176:5173",
                "http://localhost:5174",
                "https://localhost:5174"
            )
            .SetIsOriginAllowed(origin =>
            {
                var productionOrigin =
                    builder.Configuration["Cors:AllowedOrigin"];

                return !string.IsNullOrWhiteSpace(productionOrigin)
                    && string.Equals(
                        origin.TrimEnd('/'),
                        productionOrigin.TrimEnd('/'),
                        StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

app.UseCors("VueDev");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();




// =========================================================
// ROOT
// =========================================================

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        Message = "The Reister's Daughter API is running."
    });
});

// =========================================================
// AUTHENTICATION TEST
// =========================================================

app.MapGet("/api/auth/me", (
    System.Security.Claims.ClaimsPrincipal user
) =>
{
    return Results.Ok(new
    {
        Authenticated = user.Identity?.IsAuthenticated,
        Subject = user.FindFirst("sub")?.Value,
        Username = user.FindFirst("preferred_username")?.Value,
        Email = user.FindFirst("email")?.Value
    });
})
.RequireAuthorization();

// =========================================================
// AUTHORIZED API ROUTE GROUPS
// =========================================================

var managerApi = app.MapGroup("")
    .RequireAuthorization("ManagerOnly");

var employeeApi = app.MapGroup("/api/employee")
    .RequireAuthorization("ActiveUser");
// =========================================================
// EMPLOYEE - MY PUBLISHED SCHEDULE
// =========================================================

employeeApi.MapGet("/schedule", async (
    HttpContext http,
    AppDbContext db,
    DateOnly startDate) =>
{
    var subject = http.User.FindFirst("sub")?.Value;

    var user = await db.AppUsers
        .AsNoTracking()
        .FirstOrDefaultAsync(u =>
            u.ExternalId == subject && u.IsActive);

    if (user is null)
        return Results.Forbid();

    var firstStart = new DateOnly(2026, 11, 2);
    var offset = startDate.DayNumber - firstStart.DayNumber;

    if (offset < 0 || offset % 14 != 0)
        return Results.BadRequest(
            "Invalid two-week scheduling period.");

    var schedule = await db.WorkSchedules
        .AsNoTracking()
        .Where(s =>
            s.StartDate == startDate &&
            s.Status == WorkScheduleStatus.Published)
        .Select(s => new
        {
            s.StartDate,
            s.EndDate,
            Status = s.Status.ToString(),
            s.PublishedAt
        })
        .FirstOrDefaultAsync();

    if (schedule is null)
        return Results.NotFound(
            "No published schedule is available for this period.");

    var shifts = await db.ShiftAssignments
        .AsNoTracking()
        .Where(a =>
            a.AppUserId == user.Id &&
            a.WorkShift.WorkSchedule.StartDate == startDate &&
            a.WorkShift.WorkSchedule.Status ==
                WorkScheduleStatus.Published)
        .OrderBy(a => a.WorkShift.Date)
        .ThenBy(a => a.WorkShift.StartTime)
        .Select(a => new
        {
            a.WorkShift.Id,
            a.WorkShift.Date,
            a.WorkShift.StartTime,
            a.WorkShift.EndTime,
            a.WorkShift.Position
        })
        .ToListAsync();

    return Results.Ok(new
    {
        Schedule = schedule,
        Shifts = shifts
    });
});

employeeApi.MapGet("/availability", async (
    HttpContext http,
    AppDbContext db) =>
{
    var subject = http.User.FindFirst("sub")?.Value;

    var user = await db.AppUsers
        .AsNoTracking()
        .FirstOrDefaultAsync(u =>
            u.ExternalId == subject && u.IsActive);

    if (user is null)
        return Results.Forbid();

    var availability = await db.EmployeeAvailabilities
        .AsNoTracking()
        .Where(a => a.AppUserId == user.Id)
        .OrderBy(a => a.DayOfWeek)
        .ThenBy(a => a.StartTime)
        .Select(a => new
        {
            a.DayOfWeek,
            a.StartTime,
            a.EndTime
        })
        .ToListAsync();

    return Results.Ok(availability);
});

employeeApi.MapPut("/availability", async (
    HttpContext http,
    AppDbContext db,
    List<AvailabilityInput> windows) =>
{
    var subject = http.User.FindFirst("sub")?.Value;

    var user = await db.AppUsers
        .FirstOrDefaultAsync(u =>
            u.ExternalId == subject && u.IsActive);

    if (user is null)
        return Results.Forbid();

    if (windows is null || windows.Count > 28)
        return Results.BadRequest(
            "Invalid availability submission.");

    foreach (var window in windows)
    {
        if ((int)window.DayOfWeek < 0 ||
            (int)window.DayOfWeek > 6 ||
            window.EndTime <= window.StartTime)
        {
            return Results.BadRequest(
                "Invalid day or time range.");
        }
    }

    var grouped = windows
        .GroupBy(w => w.DayOfWeek);

    foreach (var day in grouped)
    {
        var sorted = day
            .OrderBy(w => w.StartTime)
            .ToList();

        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].StartTime <
                sorted[i - 1].EndTime)
            {
                return Results.BadRequest(
                    "Availability windows cannot overlap.");
            }
        }
    }

    await using var transaction =
        await db.Database.BeginTransactionAsync();

    var existing = await db.EmployeeAvailabilities
        .Where(a => a.AppUserId == user.Id)
        .ToListAsync();

    db.EmployeeAvailabilities.RemoveRange(existing);

    foreach (var window in windows)
    {
        db.EmployeeAvailabilities.Add(
            new EmployeeAvailability
            {
                AppUserId = user.Id,
                DayOfWeek = window.DayOfWeek,
                StartTime = window.StartTime,
                EndTime = window.EndTime,
                UpdatedAt = DateTimeOffset.UtcNow
            });
    }

    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(new
    {
        message = "Availability saved.",
        count = windows.Count
    });
});

//
// BIWEEKLY EMPLOYEE AVAILABILITY
//

// Read a two-week period. Missing periods are returned
// as previews without writing anything to the database.
employeeApi.MapGet("/availability/period", async (
    HttpContext http,
    AppDbContext db,
    DateOnly startDate) =>
{
    var subject = http.User.FindFirst("sub")?.Value;

    var user = await db.AppUsers
        .AsNoTracking()
        .FirstOrDefaultAsync(u =>
            u.ExternalId == subject && u.IsActive);

    if (user is null)
        return Results.Forbid();

    // Scheduling periods begin on Mondays.
    if (startDate.DayOfWeek != DayOfWeek.Monday)
        return Results.BadRequest(
            "Availability periods must start on Monday.");

    var endDate = startDate.AddDays(13);

    // October 9, 2026 is the payday anchor.
    // The first period starts November 2, 2026.
    var firstStart = new DateOnly(2026, 11, 2);
    var offset = startDate.DayNumber -
                 firstStart.DayNumber;

    if (offset < 0 || offset % 14 != 0)
        return Results.BadRequest(
            "Invalid scheduling period.");

    // The deadline is Thursday before payday,
    // 18 days before the availability period starts.
    var deadlineDate = startDate.AddDays(-18);

    var eastern = TimeZoneInfo.FindSystemTimeZoneById(
        "America/New_York");

    // Midnight Friday = end of Thursday.
    var localDeadline = new DateTime(
        deadlineDate.Year,
        deadlineDate.Month,
        deadlineDate.Day,
        0, 0, 0,
        DateTimeKind.Unspecified
    ).AddDays(1);

    var deadline = new DateTimeOffset(
        localDeadline,
        eastern.GetUtcOffset(localDeadline)
    ).ToUniversalTime();

    var period = await db.AvailabilityPeriods
        .AsNoTracking()
        .FirstOrDefaultAsync(p =>
            p.StartDate == startDate);

    var existing = period is null
        ? new List<AvailabilityEntry>()
        : await db.AvailabilityEntries
            .AsNoTracking()
            .Where(e =>
                e.AvailabilityPeriodId == period.Id &&
                e.AppUserId == user.Id)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime)
            .ToListAsync();

    var submission = period is null
        ? null
        : await db.AvailabilitySubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.AvailabilityPeriodId == period.Id &&
                s.AppUserId == user.Id);

    // If the employee has not submitted, show a
    // preview based on the preceding two-week period.
    var source = "Saved";

    if (submission is null)
    {
        var previousStart = startDate.AddDays(-14);

        var previousPeriod = await db.AvailabilityPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.StartDate == previousStart);

        var previousSubmission =
            previousPeriod is null
                ? null
                : await db.AvailabilitySubmissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.AvailabilityPeriodId ==
                            previousPeriod.Id &&
                        s.AppUserId == user.Id);

        if (previousSubmission is not null)
        {
            var previous = await db.AvailabilityEntries
                .AsNoTracking()
                .Where(e =>
                    e.AvailabilityPeriodId ==
                        previousPeriod!.Id &&
                    e.AppUserId == user.Id)
                .ToListAsync();

            existing = previous.Select(e =>
                new AvailabilityEntry
                {
                    Date = e.Date.AddDays(14),
                    StartTime = e.StartTime,
                    EndTime = e.EndTime
                }).ToList();

            source = "PreviousPeriod";
        }
        else
        {
            var weekly = await db.EmployeeAvailabilities
                .AsNoTracking()
                .Where(a => a.AppUserId == user.Id)
                .ToListAsync();

            existing = new List<AvailabilityEntry>();

            for (var i = 0; i < 14; i++)
            {
                var date = startDate.AddDays(i);

                foreach (var window in weekly.Where(w =>
                    w.DayOfWeek == date.DayOfWeek))
                {
                    existing.Add(new AvailabilityEntry
                    {
                        Date = date,
                        StartTime = window.StartTime,
                        EndTime = window.EndTime
                    });
                }
            }

            source = "WeeklyTemplate";
        }
    }

    return Results.Ok(new
    {
        startDate,
        endDate,
        deadline,
        isLocked = DateTimeOffset.UtcNow >= deadline,
        status = submission?.Status.ToString()
                 ?? "NotSubmitted",
        source,
        entries = existing.Select(e => new
        {
            e.Date,
            e.StartTime,
            e.EndTime
        })
    });
});

// Save the entire two-week period.
employeeApi.MapPut("/availability/period", async (
    HttpContext http,
    AppDbContext db,
    BiweeklyAvailabilityInput input) =>
{
    var subject = http.User.FindFirst("sub")?.Value;

    var user = await db.AppUsers
        .FirstOrDefaultAsync(u =>
            u.ExternalId == subject && u.IsActive);

    if (user is null)
        return Results.Forbid();

    var startDate = input.StartDate;

    var firstStart = new DateOnly(2026, 11, 2);
    var offset = startDate.DayNumber -
                 firstStart.DayNumber;

    if (offset < 0 || offset % 14 != 0)
        return Results.BadRequest(
            "Invalid scheduling period.");

    var endDate = startDate.AddDays(13);

    var eastern = TimeZoneInfo.FindSystemTimeZoneById(
        "America/New_York");

    // Thursday 11:59:59 PM Eastern.
    var deadlineDate = startDate.AddDays(-18);

    var localDeadline = new DateTime(
        deadlineDate.Year,
        deadlineDate.Month,
        deadlineDate.Day,
        0, 0, 0,
        DateTimeKind.Unspecified
    ).AddDays(1);

    var deadline = new DateTimeOffset(
        localDeadline,
        eastern.GetUtcOffset(localDeadline)
    ).ToUniversalTime();

    if (DateTimeOffset.UtcNow >= deadline)
        return Results.Conflict(
            "This availability period is locked.");

    if (input.Entries is null ||
        input.Entries.Count > 56)
        return Results.BadRequest(
            "Invalid availability entries.");

    foreach (var entry in input.Entries)
    {
        if (entry.Date < startDate ||
            entry.Date > endDate ||
            entry.EndTime <= entry.StartTime)
        {
            return Results.BadRequest(
                "Invalid date or time range.");
        }
    }

    foreach (var group in input.Entries
        .GroupBy(e => e.Date))
    {
        var sorted = group
            .OrderBy(e => e.StartTime)
            .ToList();

        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].StartTime <
                sorted[i - 1].EndTime)
            {
                return Results.BadRequest(
                    "Availability windows overlap.");
            }
        }
    }

    await using var transaction =
        await db.Database.BeginTransactionAsync();

    var period = await db.AvailabilityPeriods
        .FirstOrDefaultAsync(p =>
            p.StartDate == startDate);

    if (period is null)
    {
        period = new AvailabilityPeriod
        {
            StartDate = startDate,
            EndDate = endDate,
            Deadline = deadline
        };

        db.AvailabilityPeriods.Add(period);
        await db.SaveChangesAsync();
    }

    var submission = await db.AvailabilitySubmissions
        .FirstOrDefaultAsync(s =>
            s.AvailabilityPeriodId == period.Id &&
            s.AppUserId == user.Id);

    if (submission?.Status ==
        AvailabilitySubmissionStatus.ManagerOverride)
    {
        return Results.Conflict(
            "This period was overridden by a manager.");
    }

    var existing = await db.AvailabilityEntries
        .Where(e =>
            e.AvailabilityPeriodId == period.Id &&
            e.AppUserId == user.Id)
        .ToListAsync();

    db.AvailabilityEntries.RemoveRange(existing);

    foreach (var entry in input.Entries)
    {
        db.AvailabilityEntries.Add(
            new AvailabilityEntry
            {
                AvailabilityPeriodId = period.Id,
                AppUserId = user.Id,
                Date = entry.Date,
                StartTime = entry.StartTime,
                EndTime = entry.EndTime
            });
    }

    if (submission is null)
    {
        db.AvailabilitySubmissions.Add(
            new AvailabilitySubmission
            {
                AvailabilityPeriodId = period.Id,
                AppUserId = user.Id,
                Status =
                    AvailabilitySubmissionStatus.Submitted,
                UpdatedAt = DateTimeOffset.UtcNow
            });
    }
    else
    {
        submission.Status =
            AvailabilitySubmissionStatus.Submitted;
        submission.UpdatedAt = DateTimeOffset.UtcNow;
    }

    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(new
    {
        message = "Biweekly availability saved.",
        startDate,
        endDate,
        count = input.Entries.Count
    });
});



// =========================================================
// AUTHENTICATED APPLICATION USER
// =========================================================

app.MapGet(
    "/api/auth/app-user",
    async (
        System.Security.Claims.ClaimsPrincipal principal,
        AppDbContext db
    ) =>
    {
        var subject =
            principal.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Results.Forbid();
        }

        var user = await db.AppUsers
            .AsNoTracking()
            .Where(user =>
                user.ExternalId == subject &&
                user.IsActive
            )
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return Results.Forbid();
        }

        return Results.Ok(user);
    }
)
.RequireAuthorization("ActiveUser");


// =========================================================
// MENU
// =========================================================

app.MapGet("/api/menu", () =>
{
    return Results.Ok(MenuData.GetMenuItems());
});

// =========================================================
// MANAGER - BIWEEKLY EMPLOYEE AVAILABILITY
// =========================================================

managerApi.MapGet(
    "/api/admin/availability/period",
    async (AppDbContext db, DateOnly startDate) =>
    {
        var firstStart = new DateOnly(2026, 11, 2);
        var offset =
            startDate.DayNumber - firstStart.DayNumber;

        if (offset < 0 || offset % 14 != 0)
        {
            return Results.BadRequest(
                "Invalid scheduling period."
            );
        }

        var endDate = startDate.AddDays(13);

        var period = await db.AvailabilityPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.StartDate == startDate
            );

        var employees = await db.AppUsers
    .AsNoTracking()
    .Include(u => u.Positions)
    .Where(u =>
        u.IsActive &&
        u.Role == "Employee")
    .OrderBy(u => u.LastName)
    .ThenBy(u => u.FirstName)
    .ToListAsync();

        var submissions = period is null
            ? new List<AvailabilitySubmission>()
            : await db.AvailabilitySubmissions
                .AsNoTracking()
                .Where(s =>
                    s.AvailabilityPeriodId == period.Id)
                .ToListAsync();

        var entries = period is null
            ? new List<AvailabilityEntry>()
            : await db.AvailabilityEntries
                .AsNoTracking()
                .Where(e =>
                    e.AvailabilityPeriodId == period.Id)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

        var results = employees.Select(employee =>
        {
            var submission = submissions
                .FirstOrDefault(s =>
                    s.AppUserId == employee.Id);

            var employeeEntries = entries
                .Where(e =>
                    e.AppUserId == employee.Id)
                .Select(e => new
                {
                    e.Date,
                    e.StartTime,
                    e.EndTime
                })
                .ToList();

            return new
{
    employee.Id,
    employee.FirstName,
    employee.LastName,
    employee.Email,

    Positions = employee.Positions
        .Select(p => p.Position)
        .OrderBy(p => p)
        .ToArray(),

    Status = submission is null
                    ? "NotSubmitted"
                    : submission.Status.ToString(),

                SubmittedAt =
                    submission?.UpdatedAt,

                Entries = employeeEntries
            };
        }).ToList();

        return Results.Ok(new
        {
            StartDate = startDate,
            EndDate = endDate,
            Deadline = period?.Deadline,

            TotalEmployees = results.Count,

            SubmittedCount = results.Count(e =>
                e.Status == "Submitted"),

            MissingCount = results.Count(e =>
                e.Status == "NotSubmitted"),

            Employees = results
        });
    }
);

// =========================================================
// JOBS - PUBLIC APPLICATION SUBMISSION
// =========================================================

app.MapPost("/api/jobs/applications", async (
    JobApplicationRequest request,
    AppDbContext db) =>
{
    var position = request.Position?.Trim() ?? "";
    var firstName = request.FirstName?.Trim() ?? "";
    var lastName = request.LastName?.Trim() ?? "";
    var email = request.Email?.Trim().ToLowerInvariant() ?? "";
    var phone = request.Phone?.Trim() ?? "";
    var hours = request.DesiredWeeklyHours?.Trim() ?? "";
    var experience = request.Experience?.Trim() ?? "";
    var introduction = request.Introduction?.Trim() ?? "";

    var errors = new Dictionary<string, string[]>();

    if (position is not ("Barista" or "Baker" or "Cashier"))
        errors["position"] = ["Select a valid position."];

    if (firstName.Length is < 1 or > 100)
        errors["firstName"] = ["First name must be 1–100 characters."];

    if (lastName.Length is < 1 or > 100)
        errors["lastName"] = ["Last name must be 1–100 characters."];

    if (email.Length > 254 ||
        !System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
        errors["email"] = ["Enter a valid email address."];

    if (phone.Length is < 7 or > 30)
        errors["phone"] = ["Enter a valid phone number."];

    if (hours.Length > 100)
        errors["desiredWeeklyHours"] = ["Maximum 100 characters."];

    if (experience.Length > 5000)
        errors["experience"] = ["Maximum 5000 characters."];

    if (introduction.Length > 3000)
        errors["introduction"] = ["Maximum 3000 characters."];

    if (request.AvailableStartDate is { } startDate &&
        startDate < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
        errors["availableStartDate"] = ["Choose a current or future date."];

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var application = new JobApplication
    {
        Position = position,
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        Phone = phone,
        AvailableEarlyMornings = request.AvailableEarlyMornings,
        AvailableWeekends = request.AvailableWeekends,
        DesiredWeeklyHours = hours,
        AvailableStartDate = request.AvailableStartDate,
        Experience = experience,
        HasFoodSafetyCertification = request.HasFoodSafetyCertification,
        HasReliableTransportation = request.HasReliableTransportation,
        Introduction = introduction,
        Status = "New",
        CreatedAt = DateTimeOffset.UtcNow
    };

    application.Activities.Add(new JobApplicationActivity
    {
        Type = "ApplicationCreated",
        Description = "Application received."
    });

    db.JobApplications.Add(application);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        Message = "Your application has been received. Thank you!"
    });
}).RequireRateLimiting("JobApplicationSubmission");


// =========================================================
// JOBS - PUBLIC APPLICATION WITH OPTIONAL RÉSUMÉ
// =========================================================

app.MapPost("/api/jobs/applications/upload", async (
    HttpRequest httpRequest,
    AppDbContext db,
    ResumeStorageService storage,
    CancellationToken cancellationToken) =>
{
    if (!httpRequest.HasFormContentType)
        return Results.BadRequest(new
        {
            Message = "Multipart form data is required."
        });

    IFormCollection form;

    try
    {
        form = await httpRequest.ReadFormAsync(cancellationToken);
    }
    catch (Exception ex) when (
        ex is InvalidDataException or BadHttpRequestException)
    {
        return Results.BadRequest(new
        {
            Message = "Invalid application form."
        });
    }

    if (form.Keys.Count != 1 ||
        !form.ContainsKey("application") ||
        form["application"].Count != 1 ||
        form["application"][0]?.Length > 32_768)
    {
        return Results.BadRequest(new
        {
            Message = "Invalid application fields."
        });
    }

    var applicationJson = form["application"].ToString();

    JobApplicationRequest? request;

    try
    {
        request = System.Text.Json.JsonSerializer.Deserialize<JobApplicationRequest>(
            applicationJson,
            new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web));
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.BadRequest(new
        {
            Message = "Invalid application data."
        });
    }

    if (request is null)
        return Results.BadRequest(new
        {
            Message = "Application data is required."
        });

    var position = request.Position?.Trim() ?? "";
    var firstName = request.FirstName?.Trim() ?? "";
    var lastName = request.LastName?.Trim() ?? "";
    var email = request.Email?.Trim().ToLowerInvariant() ?? "";
    var phone = request.Phone?.Trim() ?? "";
    var hours = request.DesiredWeeklyHours?.Trim() ?? "";
    var experience = request.Experience?.Trim() ?? "";
    var introduction = request.Introduction?.Trim() ?? "";

    var errors = new Dictionary<string, string[]>();

    if (position is not ("Barista" or "Baker" or "Cashier"))
        errors["position"] = ["Select a valid position."];

    if (firstName.Length is < 1 or > 100)
        errors["firstName"] = ["First name must be 1–100 characters."];

    if (lastName.Length is < 1 or > 100)
        errors["lastName"] = ["Last name must be 1–100 characters."];

    if (email.Length > 254 ||
        !System.Text.RegularExpressions.Regex.IsMatch(
            email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
        errors["email"] = ["Enter a valid email address."];

    if (phone.Length is < 7 or > 30)
        errors["phone"] = ["Enter a valid phone number."];

    if (hours.Length > 100)
        errors["desiredWeeklyHours"] = ["Maximum 100 characters."];

    if (experience.Length > 5000)
        errors["experience"] = ["Maximum 5000 characters."];

    if (introduction.Length > 3000)
        errors["introduction"] = ["Maximum 3000 characters."];

    if (request.AvailableStartDate is { } startDate &&
        startDate < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
        errors["availableStartDate"] = ["Choose a current or future date."];

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var files = form.Files;

    if (files.Count > 1 ||
        (files.Count == 1 && files[0].Name != "resume"))
        return Results.BadRequest(new
        {
            Message = "Only one résumé file is allowed."
        });

    var resume = files.Count == 1 ? files[0] : null;

    if (resume is not null &&
        (resume.Length <= 0 ||
         resume.Length > ResumeStorageService.MaxFileSize))
        return Results.BadRequest(new
        {
            Message = "Résumé must be 5 MB or smaller."
        });

    string? storageKey = null;
    string? originalName = null;

    try
    {
        if (resume is not null)
        {
            var saved = await storage.SaveAsync(
                resume, cancellationToken);

            storageKey = saved.Key;
            originalName = saved.OriginalName;
        }

        var application = new JobApplication
        {
            Position = position,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            AvailableEarlyMornings = request.AvailableEarlyMornings,
            AvailableWeekends = request.AvailableWeekends,
            DesiredWeeklyHours = hours,
            AvailableStartDate = request.AvailableStartDate,
            Experience = experience,
            HasFoodSafetyCertification =
                request.HasFoodSafetyCertification,
            HasReliableTransportation =
                request.HasReliableTransportation,
            Introduction = introduction,
            ResumeStorageKey = storageKey,
            ResumeOriginalFileName = originalName,
            Status = "New",
            CreatedAt = DateTimeOffset.UtcNow
        };

        application.Activities.Add(new JobApplicationActivity
        {
            Type = "ApplicationCreated",
            Description = "Application received."
        });

        db.JobApplications.Add(application);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            Message = "Your application has been received. Thank you!"
        });
    }
    catch (InvalidDataException ex)
    {
        if (storageKey is not null)
            storage.Delete(storageKey);

        return Results.BadRequest(new { Message = ex.Message });
    }
    catch
    {
        if (storageKey is not null)
            storage.Delete(storageKey);

        throw;
    }
})
// Multipart request is submitted by a public, unauthenticated form.
// No cookie-based authentication is used for this endpoint.
.DisableAntiforgery()
.RequireRateLimiting("JobApplicationSubmission")
.WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(
    JobUploadRequestLimit));

// =========================================================
// JOBS - MANAGER APPLICATION LIST
// =========================================================

managerApi.MapGet("/api/jobs/applications", async (
    AppDbContext db,
    string? status,
    string? position) =>
{
    var query = db.JobApplications.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(status))
        query = query.Where(a => a.Status == status.Trim());

    if (!string.IsNullOrWhiteSpace(position))
        query = query.Where(a => a.Position == position.Trim());

    var applications = await query
        .OrderByDescending(a => a.CreatedAt)
        .Select(a => new
        {
            a.Id,
            a.CreatedAt,
            a.Status,
            a.Position,
            a.FirstName,
            a.LastName,
            a.Email,
            a.Phone,
            a.AvailableStartDate
        })
        .ToListAsync();

    return Results.Ok(applications);
});

// =========================================================
// JOBS - MANAGER APPLICATION DETAILS
// =========================================================

managerApi.MapGet("/api/jobs/applications/{id:int}", async (
    int id,
    AppDbContext db) =>
{
    var application = await db.JobApplications
        .AsNoTracking()
        .Where(a => a.Id == id)
        .Select(a => new
        {
            a.Id,
            a.CreatedAt,
            a.Status,
            a.Position,
            a.FirstName,
            a.LastName,
            a.Email,
            a.Phone,
            a.AvailableEarlyMornings,
            a.AvailableWeekends,
            a.DesiredWeeklyHours,
            a.AvailableStartDate,
            a.Experience,
            a.HasFoodSafetyCertification,
            a.HasReliableTransportation,
            a.Introduction,
            a.ResumeOriginalFileName,
            Notes = a.Notes
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Body,
                    n.CreatedAt
                })
                .ToList(),
            Activities = a.Activities
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new
                {
                    h.Id,
                    h.Type,
                    h.Description,
                    h.CreatedAt
                })
                .ToList()
        })
        .FirstOrDefaultAsync();

    return application is null
        ? Results.NotFound()
        : Results.Ok(application);
});


// =========================================================
// JOBS - MANAGER-ONLY RÉSUMÉ DOWNLOAD
// =========================================================

managerApi.MapGet("/api/jobs/applications/{id:int}/resume", async (
    int id,
    AppDbContext db,
    ResumeStorageService storage) =>
{
    var application = await db.JobApplications
        .AsNoTracking()
        .Where(a => a.Id == id)
        .Select(a => new
        {
            a.ResumeStorageKey,
            a.ResumeOriginalFileName
        })
        .FirstOrDefaultAsync();

    if (application is null ||
        string.IsNullOrWhiteSpace(application.ResumeStorageKey))
        return Results.NotFound();

    string path;

    try
    {
        path = storage.GetPath(application.ResumeStorageKey);
    }
    catch (InvalidDataException)
    {
        return Results.NotFound();
    }

    if (!File.Exists(path))
        return Results.NotFound();

    var extension = Path.GetExtension(path).ToLowerInvariant();

    var contentType = extension switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };

    var originalName = application.ResumeOriginalFileName;

    if (string.IsNullOrWhiteSpace(originalName))
        originalName = $"resume{extension}";

    originalName = Path.GetFileName(originalName);

    return Results.File(
        path,
        contentType: contentType,
        fileDownloadName: originalName,
        enableRangeProcessing: false);
});

// =========================================================
// JOBS - MANAGER STATUS UPDATE
// =========================================================

managerApi.MapPatch("/api/jobs/applications/{id:int}/status", async (
    int id,
    JobApplicationStatusRequest request,
    AppDbContext db) =>
{
    var status = request.Status?.Trim() ?? "";

    if (status is not (
        "New" or "Reviewing" or "Interviewing" or
        "Offered" or "Hired" or "Rejected"))
    {
        return Results.BadRequest(new
        {
            Message = "Invalid application status."
        });
    }

    var application = await db.JobApplications.FindAsync(id);

    if (application is null)
        return Results.NotFound();

    if (application.Status == status)
        return Results.Ok(new
        {
            application.Id,
            application.Status
        });

    var previousStatus = application.Status;
    application.Status = status;

    db.JobApplicationActivities.Add(new JobApplicationActivity
    {
        JobApplicationId = id,
        Type = "StatusChanged",
        Description = $"Status changed from {previousStatus} to {status}.",
        CreatedAt = DateTimeOffset.UtcNow
    });

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        application.Id,
        application.Status
    });
});

// =========================================================
// JOBS - MANAGER PRIVATE NOTES
// =========================================================

managerApi.MapPost("/api/jobs/applications/{id:int}/notes", async (
    int id,
    JobApplicationNoteRequest request,
    AppDbContext db) =>
{
    var body = request.Body?.Trim() ?? "";

    if (body.Length is < 1 or > 5000)
    {
        return Results.BadRequest(new
        {
            Message = "Note must be between 1 and 5000 characters."
        });
    }

    var exists = await db.JobApplications.AnyAsync(a => a.Id == id);

    if (!exists)
        return Results.NotFound();

    var note = new JobApplicationNote
    {
        JobApplicationId = id,
        Body = body,
        CreatedAt = DateTimeOffset.UtcNow
    };

    db.JobApplicationNotes.Add(note);

    db.JobApplicationActivities.Add(new JobApplicationActivity
    {
        JobApplicationId = id,
        Type = "NoteAdded",
        Description = "Manager added a private note.",
        CreatedAt = DateTimeOffset.UtcNow
    });

    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/jobs/applications/{id}/notes/{note.Id}",
        new
        {
            note.Id,
            note.Body,
            note.CreatedAt
        });
});

// =========================================================
// INQUIRIES - CREATE
// =========================================================

app.MapPost(
    "/api/inquiries",
    async (Inquiry inquiry, AppDbContext db) =>
    {
        inquiry.Id = 0;
        inquiry.CreatedAt = DateTime.UtcNow;
        inquiry.Status = "New";

        var normalizedEmail =
            inquiry.Email.Trim().ToLowerInvariant();

        var customer = await db.Customers
            .FirstOrDefaultAsync(
                customer =>
                    customer.Email.Trim().ToLower() == normalizedEmail
            );

        if (customer is null)
        {
            customer = new Customer
            {
                FirstName = inquiry.FirstName.Trim(),
                LastName = inquiry.LastName.Trim(),
                Company = inquiry.Company?.Trim(),
                Email = normalizedEmail,
                Phone = inquiry.Phone.Trim()
            };

            db.Customers.Add(customer);
        }
        else
        {
            customer.FirstName = inquiry.FirstName.Trim();
            customer.LastName = inquiry.LastName.Trim();
            customer.Company = inquiry.Company?.Trim();
            customer.Phone = inquiry.Phone.Trim();
        }

        inquiry.Customer = customer;

        db.Inquiries.Add(inquiry);

        await db.SaveChangesAsync();

        db.InquiryActivities.Add(
            new InquiryActivity
            {
                InquiryId = inquiry.Id,
                Type = "InquiryCreated",
                Description = "Inquiry received."
            }
        );

        await db.SaveChangesAsync();

        Console.WriteLine(
            $"New inquiry #{inquiry.Id} " +
            $"from customer #{customer.Id}: " +
            $"{inquiry.FirstName} {inquiry.LastName}"
        );

        return Results.Created(
            $"/api/inquiries/{inquiry.Id}",
            new
            {
                inquiry.Id,
                inquiry.CustomerId,
                inquiry.Status,
                inquiry.CreatedAt,
                Message = "Inquiry received."
            }
        );
    }
);


// =========================================================
// INQUIRIES - GET ALL
// =========================================================

managerApi.MapGet(
    "/api/inquiries",
    async (AppDbContext db) =>
    {
        var inquiries = await db.Inquiries
            .AsNoTracking()
            .OrderByDescending(inquiry => inquiry.CreatedAt)
            .ToListAsync();

        return Results.Ok(inquiries);
    }
);


// =========================================================
// INQUIRIES - GET ONE
// =========================================================

managerApi.MapGet(
    "/api/inquiries/{id:int}",
    async (int id, AppDbContext db) =>
    {
        var inquiry = await db.Inquiries
            .AsNoTracking()
            .Where(inquiry => inquiry.Id == id)
            .Select(inquiry => new
            {
                inquiry.Id,
                inquiry.CustomerId,
                inquiry.CreatedAt,
                inquiry.Status,

                inquiry.EventType,
                inquiry.Services,

                inquiry.FirstName,
                inquiry.LastName,
                inquiry.Company,
                inquiry.Email,
                inquiry.Phone,

                inquiry.EventDate,
                inquiry.EventTime,
                inquiry.GuestCount,

                inquiry.CateringType,
                inquiry.ServiceType,
                inquiry.DeliveryAddress,
                inquiry.Packaging,

                inquiry.DietaryNeeds,
                inquiry.OtherDietaryNeeds,

                inquiry.Recurring,
                inquiry.Details,

                Customer =
                    inquiry.Customer == null
                        ? null
                        : new
                        {
                            inquiry.Customer.Id,
                            inquiry.Customer.FirstName,
                            inquiry.Customer.LastName,
                            inquiry.Customer.Company,
                            inquiry.Customer.Email,
                            inquiry.Customer.Phone,
                            inquiry.Customer.CreatedAt
                        },

                Notes = inquiry.Notes
                    .OrderByDescending(note => note.CreatedAt)
                    .Select(note => new
                    {
                        note.Id,
                        note.Body,
                        note.CreatedAt
                    })
                    .ToList(),

                Activities = inquiry.Activities
                    .OrderByDescending(activity => activity.CreatedAt)
                    .Select(activity => new
                    {
                        activity.Id,
                        activity.Type,
                        activity.Description,
                        activity.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return inquiry is null
            ? Results.NotFound()
            : Results.Ok(inquiry);
    }
);

// =========================================================
// INQUIRIES - LINK CUSTOMER
// =========================================================

managerApi.MapPost(
    "/api/inquiries/{id:int}/link-customer",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var inquiry = await db.Inquiries
            .Include(inquiry => inquiry.Customer)
            .FirstOrDefaultAsync(inquiry =>
                inquiry.Id == id
            );

        if (inquiry is null)
        {
            return Results.NotFound(new
            {
                Message = "Inquiry not found."
            });
        }

        if (inquiry.CustomerId is not null)
        {
            return Results.Ok(new
            {
                inquiry.Id,
                inquiry.CustomerId,
                Message =
                    "Inquiry is already linked to a customer."
            });
        }

        var normalizedEmail =
            inquiry.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Results.BadRequest(new
            {
                Message =
                    "Inquiry does not contain an email address."
            });
        }

        var customer = await db.Customers
            .FirstOrDefaultAsync(customer =>
                customer.Email.Trim().ToLower() ==
                normalizedEmail
            );

        var customerCreated = false;

        if (customer is null)
        {
            customer = new Customer
            {
                FirstName = inquiry.FirstName.Trim(),
                LastName = inquiry.LastName.Trim(),
                Company = inquiry.Company?.Trim(),
                Email = normalizedEmail,
                Phone = inquiry.Phone.Trim()
            };

            db.Customers.Add(customer);

            customerCreated = true;
        }

        inquiry.Customer = customer;

        db.InquiryActivities.Add(
            new InquiryActivity
            {
                InquiryId = inquiry.Id,
                Type = "CustomerLinked",
                Description = customerCreated
                    ? "Customer record created and linked to inquiry."
                    : "Inquiry linked to existing customer."
            }
        );

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            inquiry.Id,
            inquiry.CustomerId,
            Customer = new
            {
                customer.Id,
                customer.FirstName,
                customer.LastName,
                customer.Company,
                customer.Email,
                customer.Phone
            },
            CustomerCreated = customerCreated,
            Message = customerCreated
                ? "Customer created and linked."
                : "Existing customer linked."
        });
    }
);

// =========================================================
// INQUIRIES - UPDATE STATUS
// =========================================================

managerApi.MapPatch(
    "/api/inquiries/{id:int}/status",
    async (
        int id,
        UpdateInquiryStatusRequest request,
        AppDbContext db
    ) =>
    {
        var allowedStatuses = new[]
        {
            "New",
            "Contacted",
            "Quoted",
            "Booked",
            "Closed"
        };

        if (!allowedStatuses.Contains(request.Status))
        {
            return Results.BadRequest(new
            {
                Message = "Invalid status."
            });
        }

        var inquiry = await db.Inquiries.FindAsync(id);

        if (inquiry is null)
        {
            return Results.NotFound();
        }

        var previousStatus = inquiry.Status;

        if (previousStatus == request.Status)
        {
            return Results.Ok(new
            {
                inquiry.Id,
                inquiry.Status
            });
        }

        inquiry.Status = request.Status;

        db.InquiryActivities.Add(
            new InquiryActivity
            {
                InquiryId = inquiry.Id,
                Type = "StatusChanged",
                Description =
                    $"Status changed from {previousStatus} " +
                    $"to {request.Status}."
            }
        );

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            inquiry.Id,
            inquiry.Status
        });
    }
);


// =========================================================
// INQUIRIES - ADD INTERNAL NOTE
// =========================================================

managerApi.MapPost(
    "/api/inquiries/{id:int}/notes",
    async (
        int id,
        CreateInquiryNoteRequest request,
        AppDbContext db
    ) =>
    {
        var inquiry = await db.Inquiries.FindAsync(id);

        if (inquiry is null)
        {
            return Results.NotFound();
        }

        var body = request.Body?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new
            {
                Message = "Note cannot be empty."
            });
        }

        var note = new InquiryNote
        {
            InquiryId = inquiry.Id,
            Body = body
        };

        db.InquiryNotes.Add(note);

        db.InquiryActivities.Add(
            new InquiryActivity
            {
                InquiryId = inquiry.Id,
                Type = "NoteAdded",
                Description = "Internal note added."
            }
        );

        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/inquiries/{inquiry.Id}/notes/{note.Id}",
            new
            {
                note.Id,
                note.InquiryId,
                note.Body,
                note.CreatedAt
            }
        );
    }
);


// =========================================================
// QUOTES - CREATE
// =========================================================

managerApi.MapPost(
    "/api/inquiries/{id:int}/quotes",
    async (
        int id,
        CreateQuoteRequest request,
        AppDbContext db
    ) =>
    {
        var inquiry = await db.Inquiries.FindAsync(id);

        if (inquiry is null)
        {
            return Results.NotFound(new
            {
                Message = "Inquiry not found."
            });
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return Results.BadRequest(new
            {
                Message = "A quote must contain at least one item."
            });
        }

        if (request.Items.Any(item =>
            string.IsNullOrWhiteSpace(item.Description)))
        {
            return Results.BadRequest(new
            {
                Message = "Every quote item needs a description."
            });
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            return Results.BadRequest(new
            {
                Message =
                    "Item quantities must be greater than zero."
            });
        }

        if (request.Items.Any(item => item.UnitPrice < 0))
        {
            return Results.BadRequest(new
            {
                Message = "Item prices cannot be negative."
            });
        }

        if (request.Tax < 0)
        {
            return Results.BadRequest(new
            {
                Message = "Tax cannot be negative."
            });
        }

        var quote = new Quote
        {
            InquiryId = inquiry.Id,

            Status = "Draft",

            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,

            ExpiresAt = request.ExpiresAt?.ToUniversalTime(),

            CustomerMessage =
                string.IsNullOrWhiteSpace(
                    request.CustomerMessage
                )
                    ? null
                    : request.CustomerMessage.Trim(),

            Tax = decimal.Round(
                request.Tax,
                2,
                MidpointRounding.AwayFromZero
            )
        };

        for (
            var index = 0;
            index < request.Items.Count;
            index++
        )
        {
            var requestItem = request.Items[index];

            quote.Items.Add(
                new QuoteItem
                {
                    Description =
                        requestItem.Description.Trim(),

                    // Whole-number quantities only.
                    Quantity = requestItem.Quantity,

                    UnitPrice = decimal.Round(
                        requestItem.UnitPrice,
                        2,
                        MidpointRounding.AwayFromZero
                    ),

                    SortOrder = index
                }
            );
        }

        quote.Subtotal = decimal.Round(
            quote.Items.Sum(
                item =>
                    item.Quantity * item.UnitPrice
            ),
            2,
            MidpointRounding.AwayFromZero
        );

        quote.Total = decimal.Round(
            quote.Subtotal + quote.Tax,
            2,
            MidpointRounding.AwayFromZero
        );

        db.Quotes.Add(quote);

        // First save generates the PostgreSQL identity ID.
        await db.SaveChangesAsync();

        quote.QuoteNumber =
            $"Q-{quote.Id:D6}";

        db.InquiryActivities.Add(
            new InquiryActivity
            {
                InquiryId = inquiry.Id,
                Type = "QuoteCreated",
                Description =
                    $"Quote {quote.QuoteNumber} " +
                    $"created for {quote.Total:C}."
            }
        );

        await db.SaveChangesAsync();

        var response = new
        {
            quote.Id,
            quote.InquiryId,
            quote.QuoteNumber,
            quote.Status,

            quote.CreatedAt,
            quote.UpdatedAt,
            quote.ExpiresAt,

            quote.CustomerMessage,

            quote.Subtotal,
            quote.Tax,
            quote.Total,

            Items = quote.Items
                .OrderBy(item => item.SortOrder)
                .Select(item => new
                {
                    item.Id,
                    item.Description,

                    item.Quantity,
                    item.UnitPrice,

                    LineTotal = decimal.Round(
                        item.Quantity *
                        item.UnitPrice,

                        2,
                        MidpointRounding.AwayFromZero
                    ),

                    item.SortOrder
                })
                .ToList()
        };

        return Results.Created(
            $"/api/quotes/{quote.Id}",
            response
        );
    }
);


// =========================================================
// QUOTES - GET ONE
// =========================================================

managerApi.MapGet(
    "/api/quotes/{id:int}",
    async (int id, AppDbContext db) =>
    {
        var quote = await db.Quotes
            .AsNoTracking()
            .Where(quote => quote.Id == id)
            .Select(quote => new
            {
                quote.Id,
                quote.InquiryId,
                quote.QuoteNumber,
                quote.Status,

                quote.CreatedAt,
                quote.UpdatedAt,
                quote.ExpiresAt,

                quote.CustomerMessage,

                quote.Subtotal,
                quote.Tax,
                quote.Total,

                Items = quote.Items
                    .OrderBy(item => item.SortOrder)
                    .Select(item => new
                    {
                        item.Id,
                        item.Description,

                        item.Quantity,
                        item.UnitPrice,

                        LineTotal =
                            item.Quantity *
                            item.UnitPrice,

                        item.SortOrder
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return quote is null
            ? Results.NotFound()
            : Results.Ok(quote);
    }
);

// QUOTES - UPDATE DRAFT
managerApi.MapPut(
    "/api/quotes/{id:int}",
    async (
        int id,
        UpdateQuoteRequest request,
        AppDbContext db
    ) =>
    {
        var quote = await db.Quotes
            .Include(quote => quote.Items)
            .FirstOrDefaultAsync(quote => quote.Id == id);

        if (quote is null)
        {
            return Results.NotFound(new
            {
                Message = "Quote not found."
            });
        }

        if (quote.Status != "Draft")
        {
            return Results.BadRequest(new
            {
                Message = "Only draft quotes can be edited."
            });
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return Results.BadRequest(new
            {
                Message = "A quote must contain at least one item."
            });
        }

        if (request.Items.Any(item =>
            string.IsNullOrWhiteSpace(item.Description)))
        {
            return Results.BadRequest(new
            {
                Message = "Every quote item needs a description."
            });
        }

        if (request.Items.Any(item =>
            item.Quantity <= 0))
        {
            return Results.BadRequest(new
            {
                Message =
                    "Item quantities must be greater than zero."
            });
        }

        if (request.Items.Any(item =>
            item.UnitPrice < 0))
        {
            return Results.BadRequest(new
            {
                Message = "Item prices cannot be negative."
            });
        }

        if (request.Tax < 0)
        {
            return Results.BadRequest(new
            {
                Message = "Tax cannot be negative."
            });
        }

        // Replace the existing line items with the
        // current contents of the editor.
        db.QuoteItems.RemoveRange(quote.Items);
        quote.Items.Clear();

        for (
            var index = 0;
            index < request.Items.Count;
            index++
        )
        {
            var requestItem = request.Items[index];

            quote.Items.Add(
                new QuoteItem
                {
                    Description =
                        requestItem.Description.Trim(),
                    Quantity = requestItem.Quantity,
                    UnitPrice = decimal.Round(
                        requestItem.UnitPrice,
                        2,
                        MidpointRounding.AwayFromZero
                    ),
                    SortOrder = index
                }
            );
        }

       quote.ExpiresAt =
    request.ExpiresAt?.ToUniversalTime();

        quote.CustomerMessage =
            string.IsNullOrWhiteSpace(
                request.CustomerMessage
            )
                ? null
                : request.CustomerMessage.Trim();

        quote.Tax = decimal.Round(
            request.Tax,
            2,
            MidpointRounding.AwayFromZero
        );

        quote.Subtotal = decimal.Round(
            quote.Items.Sum(
                item =>
                    item.Quantity * item.UnitPrice
            ),
            2,
            MidpointRounding.AwayFromZero
        );

        quote.Total = decimal.Round(
            quote.Subtotal + quote.Tax,
            2,
            MidpointRounding.AwayFromZero
        );

        quote.UpdatedAt = DateTimeOffset.UtcNow;

        db.InquiryActivities.Add(
            new InquiryActivity
            {
                InquiryId = quote.InquiryId,
                Type = "QuoteUpdated",
                Description =
                    $"Quote {quote.QuoteNumber} updated."
            }
        );

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            quote.Id,
            quote.InquiryId,
            quote.QuoteNumber,
            quote.Status,
            quote.CreatedAt,
            quote.UpdatedAt,
            quote.ExpiresAt,
            quote.CustomerMessage,
            quote.Subtotal,
            quote.Tax,
            quote.Total,

            Items = quote.Items
                .OrderBy(item => item.SortOrder)
                .Select(item => new
                {
                    item.Id,
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,

                    LineTotal = decimal.Round(
                        item.Quantity *
                        item.UnitPrice,
                        2,
                        MidpointRounding.AwayFromZero
                    ),

                    item.SortOrder
                })
                .ToList()
        });
    }
);


// =========================================================
// QUOTES - SEND
// =========================================================

managerApi.MapPost(
    "/api/quotes/{id:int}/send",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var quote = await db.Quotes
            .FirstOrDefaultAsync(quote => quote.Id == id);

        if (quote is null)
        {
            return Results.NotFound(new
            {
                Message = "Quote not found."
            });
        }

        if (
            quote.Status != "Draft" &&
            quote.Status != "Sent"
        )
        {
            return Results.BadRequest(new
            {
                Message =
                    "Only draft or sent quotes can generate a public link."
            });
        }

        // Generate the secure public token only once.
        if (string.IsNullOrWhiteSpace(quote.PublicToken))
        {
            quote.PublicToken =
                Convert.ToHexString(
                    System.Security.Cryptography.RandomNumberGenerator
                        .GetBytes(32)
                ).ToLowerInvariant();
        }

        // Only record the send activity the first time.
        if (quote.Status == "Draft")
        {
            quote.Status = "Sent";

            db.InquiryActivities.Add(
                new InquiryActivity
                {
                    InquiryId = quote.InquiryId,
                    Type = "QuoteSent",
                    Description =
                        $"Quote {quote.QuoteNumber} sent to customer."
                }
            );
        }

        quote.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            quote.Id,
            quote.InquiryId,
            quote.QuoteNumber,
            quote.Status,
            quote.PublicToken,
            quote.UpdatedAt,
            Message = "Quote sent."
        });
    }
);
// =========================================================
// QUOTES - GET ALL FOR AN INQUIRY
// =========================================================

managerApi.MapGet(
    "/api/inquiries/{id:int}/quotes",
    async (int id, AppDbContext db) =>
    {
        var inquiryExists = await db.Inquiries
            .AsNoTracking()
            .AnyAsync(inquiry => inquiry.Id == id);

        if (!inquiryExists)
        {
            return Results.NotFound();
        }

        var quotes = await db.Quotes
            .AsNoTracking()
            .Where(quote => quote.InquiryId == id)
            .OrderByDescending(
                quote => quote.CreatedAt
            )
            .Select(quote => new
            {
                quote.Id,
                quote.QuoteNumber,
                quote.Status,

                quote.CreatedAt,
                quote.UpdatedAt,
                quote.ExpiresAt,

                quote.Subtotal,
                quote.Tax,
                quote.Total,

                ItemCount = quote.Items.Count
            })
            .ToListAsync();

        return Results.Ok(quotes);
    }
);
// PUBLIC QUOTES - GET BY TOKEN
app.MapGet(
    "/api/public/quotes/{token}",
    async (
        string token,
        AppDbContext db
    ) =>
    {
        var quote = await db.Quotes
            .AsNoTracking()
            .Where(quote =>
                quote.PublicToken == token &&
                quote.Status != "Draft"
            )
            .Select(quote => new
            {
                quote.QuoteNumber,
                quote.Status,
                quote.CreatedAt,
                quote.ExpiresAt,
                quote.CustomerMessage,
                quote.Subtotal,
                quote.Tax,
                quote.Total,

                Customer = new
                {
                    quote.Inquiry.FirstName,
                    quote.Inquiry.LastName,
                    quote.Inquiry.Company
                },

                Event = new
                {
                    quote.Inquiry.EventType,
                    quote.Inquiry.EventDate,
                    quote.Inquiry.EventTime,
                    quote.Inquiry.GuestCount
                },

                Items = quote.Items
                    .OrderBy(item => item.SortOrder)
                    .Select(item => new
                    {
                        item.Description,
                        item.Quantity,
                        item.UnitPrice,

                        LineTotal =
                            item.Quantity *
                            item.UnitPrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return quote is null
            ? Results.NotFound(new
            {
                Message = "Quote not found."
            })
            : Results.Ok(quote);
    }
);
// =========================================================
// PUBLIC QUOTES - ACCEPT
// =========================================================

app.MapPost(
    "/api/public/quotes/{token}/accept",
    async (
        string token,
        AppDbContext db
    ) =>
    {
        var quote = await db.Quotes
            .Include(quote => quote.Inquiry)
            .FirstOrDefaultAsync(quote =>
                quote.PublicToken == token
            );

        if (quote is null)
        {
            return Results.NotFound(new
            {
                Message = "Quote not found."
            });
        }

        // Only Sent or already-Accepted quotes are valid here.
        if (
            quote.Status != "Sent" &&
            quote.Status != "Accepted"
        )
        {
            return Results.BadRequest(new
            {
                Message =
                    "Only sent quotes can be accepted."
            });
        }

        // Only check expiration when accepting for the first time.
        if (
            quote.Status == "Sent" &&
            quote.ExpiresAt.HasValue &&
            quote.ExpiresAt.Value < DateTimeOffset.UtcNow
        )
        {
            return Results.BadRequest(new
            {
                Message = "This quote has expired."
            });
        }

        var newlyAccepted =
            quote.Status == "Sent";

        if (newlyAccepted)
        {
            quote.Status = "Accepted";
            quote.UpdatedAt = DateTimeOffset.UtcNow;

            db.InquiryActivities.Add(
                new InquiryActivity
                {
                    InquiryId = quote.InquiryId,
                    Type = "QuoteAccepted",
                    Description =
                        $"Quote {quote.QuoteNumber} accepted by customer."
                }
            );
        }

        // A quote can create only one booking.
        var booking = await db.Bookings
            .FirstOrDefaultAsync(booking =>
                booking.QuoteId == quote.Id
            );

        var bookingCreated = false;

        if (booking is null)
        {
            booking = new Booking
            {
                QuoteId = quote.Id,
                InquiryId = quote.InquiryId,
                CustomerId = quote.Inquiry.CustomerId,

                Status = "Confirmed",

                EventType =
                    quote.Inquiry.EventType,

                EventDate =
                    quote.Inquiry.EventDate,

                EventTime =
                    quote.Inquiry.EventTime,

                GuestCount =
                    quote.Inquiry.GuestCount,

                Total = quote.Total
            };

            db.Bookings.Add(booking);

            db.InquiryActivities.Add(
                new InquiryActivity
                {
                    InquiryId = quote.InquiryId,
                    Type = "BookingCreated",
                    Description =
                        $"Booking created from {quote.QuoteNumber}."
                }
            );

            bookingCreated = true;
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            quote.QuoteNumber,
            quote.Status,
            quote.UpdatedAt,

            Booking = new
            {
                booking.Id,
                booking.Status,
                booking.EventType,
                booking.EventDate,
                booking.EventTime,
                booking.GuestCount,
                booking.Total
            },

            BookingCreated = bookingCreated,

            Message =
                newlyAccepted
                    ? "Quote accepted and booking created successfully."
                    : bookingCreated
                        ? "Booking created for accepted quote."
                        : "Quote has already been accepted and booked."
        });
    }
);
// =========================================================
// BOOKINGS - GET ALL
// =========================================================

managerApi.MapGet(
    "/api/bookings",
    async (AppDbContext db) =>
    {
        var bookings = await db.Bookings
            .AsNoTracking()
            .OrderBy(booking => booking.EventDate)
            .ThenBy(booking => booking.EventTime)
            .Select(booking => new
            {
                booking.Id,
                booking.Status,
                booking.EventType,
                booking.EventDate,
                booking.EventTime,
                booking.GuestCount,
                booking.Total,
                booking.CreatedAt,
                booking.UpdatedAt,

                Quote = new
                {
                    booking.QuoteId,
                    booking.Quote.QuoteNumber
                },

                Customer = booking.Customer == null
                    ? null
                    : new
                    {
                        booking.Customer.Id,
                        booking.Customer.FirstName,
                        booking.Customer.LastName,
                        booking.Customer.Company,
                        booking.Customer.Email,
                        booking.Customer.Phone
                    },

                Inquiry = new
                {
                    booking.InquiryId,
                    booking.Inquiry.Services,
                    booking.Inquiry.CateringType,
                    booking.Inquiry.ServiceType,
                    booking.Inquiry.DeliveryAddress,
                    booking.Inquiry.Packaging,
                    booking.Inquiry.DietaryNeeds,
                    booking.Inquiry.OtherDietaryNeeds,
                    booking.Inquiry.Details
                }
            })
            .ToListAsync();

        return Results.Ok(bookings);
    }
);
// =========================================================
// BOOKINGS - GET ONE
// =========================================================

managerApi.MapGet(
    "/api/bookings/{id:int}",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .Where(booking => booking.Id == id)
            .Select(booking => new
            {
                booking.Id,
                booking.Status,
                booking.EventType,
                booking.EventDate,
                booking.EventTime,
                booking.GuestCount,
                booking.Total,
                booking.InternalNotes,
                booking.CreatedAt,
                booking.UpdatedAt,

                Quote = new
                {
                    booking.QuoteId,
                    booking.Quote.QuoteNumber,
                    booking.Quote.Status,
                    booking.Quote.CustomerMessage,
                    booking.Quote.Subtotal,
                    booking.Quote.Tax,
                    booking.Quote.Total,

                    Items = booking.Quote.Items
                        .OrderBy(item => item.SortOrder)
                        .Select(item => new
                        {
                            item.Id,
                            item.Description,
                            item.Quantity,
                            item.UnitPrice,
                            LineTotal =
                                item.Quantity * item.UnitPrice
                        })
                        .ToList()
                },

                Customer = booking.Customer == null
                    ? null
                    : new
                    {
                        booking.Customer.Id,
                        booking.Customer.FirstName,
                        booking.Customer.LastName,
                        booking.Customer.Company,
                        booking.Customer.Email,
                        booking.Customer.Phone
                    },

                Inquiry = new
                {
                    booking.InquiryId,
                    booking.Inquiry.EventType,
                    booking.Inquiry.Services,
                    booking.Inquiry.CateringType,
                    booking.Inquiry.ServiceType,
                    booking.Inquiry.DeliveryAddress,
                    booking.Inquiry.Packaging,
                    booking.Inquiry.DietaryNeeds,
                    booking.Inquiry.OtherDietaryNeeds,
                    booking.Inquiry.Recurring,
                    booking.Inquiry.Details
                }
            })
            .FirstOrDefaultAsync();

        return booking is null
            ? Results.NotFound(new
            {
                Message = "Booking not found."
            })
            : Results.Ok(booking);
    }
);
// =========================================================
// BOOKINGS - UPDATE STATUS
// =========================================================

managerApi.MapPatch(
    "/api/bookings/{id:int}/status",
    async (
        int id,
        UpdateBookingStatusRequest request,
        AppDbContext db
    ) =>
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(booking =>
                booking.Id == id
            );

        if (booking is null)
        {
            return Results.NotFound(new
            {
                Message = "Booking not found."
            });
        }

        var allowedStatuses = new[]
        {
            "Confirmed",
            "In Preparation",
            "Completed",
            "Cancelled"
        };

        if (!allowedStatuses.Contains(request.Status))
        {
            return Results.BadRequest(new
            {
                Message = "Invalid booking status."
            });
        }

        if (booking.Status == request.Status)
        {
            return Results.Ok(new
            {
                booking.Id,
                booking.Status,
                booking.UpdatedAt,
                Message = "Booking status is already set."
            });
        }

        var previousStatus = booking.Status;

        booking.Status = request.Status;
        booking.UpdatedAt = DateTimeOffset.UtcNow;

        db.InquiryActivities.Add(
            new InquiryActivity
            {
                InquiryId = booking.InquiryId,
                Type = "BookingStatusChanged",
                Description =
                    $"Booking #{booking.Id} changed from {previousStatus} to {booking.Status}."
            }
        );

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            booking.Id,
            booking.Status,
            booking.UpdatedAt,
            Message = "Booking status updated."
        });
    }
);


// =========================================================
// BOOKINGS - UPDATE INTERNAL NOTES
// =========================================================

managerApi.MapPatch(
    "/api/bookings/{id:int}/notes",
    async (
        int id,
        UpdateBookingNotesRequest request,
        AppDbContext db
    ) =>
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(booking =>
                booking.Id == id
            );

        if (booking is null)
        {
            return Results.NotFound(new
            {
                Message = "Booking not found."
            });
        }

        booking.InternalNotes =
            string.IsNullOrWhiteSpace(request.InternalNotes)
                ? null
                : request.InternalNotes.Trim();

        booking.UpdatedAt = DateTimeOffset.UtcNow;

        db.InquiryActivities.Add(
            new InquiryActivity
            {
                InquiryId = booking.InquiryId,
                Type = "BookingNotesUpdated",
                Description =
                    $"Internal notes updated for Booking #{booking.Id}."
            }
        );

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            booking.Id,
            booking.InternalNotes,
            booking.UpdatedAt,
            Message = "Internal notes updated."
        });
    }
);

// =========================================================
// CUSTOMERS - GET ALL
// =========================================================

managerApi.MapGet(
    "/api/customers",
    async (AppDbContext db) =>
    {
        var customers = await db.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .Select(customer => new
            {
                customer.Id,
                customer.CreatedAt,
                customer.FirstName,
                customer.LastName,
                customer.Company,
                customer.Email,
                customer.Phone,

                InquiryCount = customer.Inquiries.Count,

                QuoteCount = customer.Inquiries
                    .SelectMany(inquiry => inquiry.Quotes)
                    .Count(),

                BookingCount = db.Bookings.Count(
                    booking =>
                        booking.CustomerId == customer.Id
                )
            })
            .ToListAsync();

        return Results.Ok(customers);
    }
);


// =========================================================
// CUSTOMERS - GET ONE
// =========================================================

managerApi.MapGet(
    "/api/customers/{id:int}",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var customer = await db.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == id)
            .Select(customer => new
            {
                customer.Id,
                customer.CreatedAt,
                customer.FirstName,
                customer.LastName,
                customer.Company,
                customer.Email,
                customer.Phone,

                Inquiries = customer.Inquiries
                    .OrderByDescending(inquiry => inquiry.CreatedAt)
                    .Select(inquiry => new
                    {
                        inquiry.Id,
                        inquiry.CreatedAt,
                        inquiry.Status,
                        inquiry.EventType,
                        inquiry.EventDate,
                        inquiry.EventTime,
                        inquiry.GuestCount,
                        inquiry.Services,
                        inquiry.Details,

                        Quotes = inquiry.Quotes
                            .OrderByDescending(
                                quote => quote.CreatedAt
                            )
                            .Select(quote => new
                            {
                                quote.Id,
                                quote.QuoteNumber,
                                quote.Status,
                                quote.CreatedAt,
                                quote.Total
                            })
                            .ToList()
                    })
                    .ToList(),

                Bookings = db.Bookings
                    .Where(booking =>
                        booking.CustomerId == customer.Id
                    )
                    .OrderByDescending(
                        booking => booking.EventDate
                    )
                    .ThenByDescending(
                        booking => booking.EventTime
                    )
                    .Select(booking => new
                    {
                        booking.Id,
                        booking.Status,
                        booking.EventType,
                        booking.EventDate,
                        booking.EventTime,
                        booking.GuestCount,
                        booking.Total,

                        booking.QuoteId,
                        booking.Quote.QuoteNumber,

                        booking.InquiryId
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return customer is null
            ? Results.NotFound(new
            {
                Message = "Customer not found."
            })
            : Results.Ok(customer);
    }
);

// =========================================================
// ADMIN DASHBOARD
// =========================================================

managerApi.MapGet(
    "/api/admin/dashboard",
    async (AppDbContext db) =>
    {
        var now = DateTimeOffset.UtcNow;

        var customerCount = await db.Customers
            .AsNoTracking()
            .CountAsync();

        var inquiryCount = await db.Inquiries
            .AsNoTracking()
            .CountAsync();

        var newInquiryCount = await db.Inquiries
            .AsNoTracking()
            .CountAsync(inquiry =>
                inquiry.Status == "New"
            );

        var draftQuoteCount = await db.Quotes
            .AsNoTracking()
            .CountAsync(quote =>
                quote.Status == "Draft"
            );

        var sentQuoteCount = await db.Quotes
            .AsNoTracking()
            .CountAsync(quote =>
                quote.Status == "Sent"
            );

        var acceptedQuoteCount = await db.Quotes
            .AsNoTracking()
            .CountAsync(quote =>
                quote.Status == "Accepted"
            );

        var confirmedBookingCount = await db.Bookings
            .AsNoTracking()
            .CountAsync(booking =>
                booking.Status == "Confirmed"
            );

        var inPreparationBookingCount = await db.Bookings
            .AsNoTracking()
            .CountAsync(booking =>
                booking.Status == "In Preparation"
            );

        var recentInquiries = await db.Inquiries
            .AsNoTracking()
            .OrderByDescending(inquiry =>
                inquiry.CreatedAt
            )
            .Take(5)
            .Select(inquiry => new
            {
                inquiry.Id,
                inquiry.CreatedAt,
                inquiry.Status,
                inquiry.EventType,
                inquiry.EventDate,
                inquiry.EventTime,
                inquiry.GuestCount,

                Customer = inquiry.Customer == null
                    ? new
                    {
                        Id = (int?)null,
                        inquiry.FirstName,
                        inquiry.LastName,
                        inquiry.Company
                    }
                    : new
                    {
                        Id = (int?)inquiry.Customer.Id,
                        inquiry.Customer.FirstName,
                        inquiry.Customer.LastName,
                        inquiry.Customer.Company
                    }
            })
            .ToListAsync();

        var upcomingBookings = await db.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.Status != "Completed" &&
                booking.Status != "Cancelled"
            )
            .OrderBy(booking =>
                booking.EventDate
            )
            .ThenBy(booking =>
                booking.EventTime
            )
            .Take(5)
            .Select(booking => new
            {
                booking.Id,
                booking.Status,
                booking.EventType,
                booking.EventDate,
                booking.EventTime,
                booking.GuestCount,
                booking.Total,

                Customer = booking.Customer == null
                    ? null
                    : new
                    {
                        booking.Customer.Id,
                        booking.Customer.FirstName,
                        booking.Customer.LastName,
                        booking.Customer.Company
                    },

                booking.QuoteId,
                booking.Quote.QuoteNumber,
                booking.InquiryId
            })
            .ToListAsync();

        var quotesNeedingAttention = await db.Quotes
            .AsNoTracking()
            .Where(quote =>
                quote.Status == "Draft" ||
                quote.Status == "Sent"
            )
            .OrderByDescending(quote =>
                quote.UpdatedAt
            )
            .Take(5)
            .Select(quote => new
            {
                quote.Id,
                quote.QuoteNumber,
                quote.Status,
                quote.UpdatedAt,
                quote.ExpiresAt,
                quote.Total,
                quote.InquiryId,

                Customer = new
                {
                    quote.Inquiry.CustomerId,
                    quote.Inquiry.FirstName,
                    quote.Inquiry.LastName,
                    quote.Inquiry.Company
                }
            })
            .ToListAsync();
        var pendingReservationCount =
            await db.EventReservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.Status == "Pending"
                );

        var pendingReservations =
            await db.EventReservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Status == "Pending"
                )
                .OrderByDescending(reservation =>
                    reservation.CreatedAt
                )
                .Take(5)
                .Select(reservation => new
                {
                    ReservationId = reservation.Id,
                    reservation.EventId,
                    EventTitle =
                        reservation.Event.Title,
                    reservation.FirstName,
                    reservation.LastName,
                    reservation.Email,
                    reservation.Phone,
                    reservation.GuestCount,
                    reservation.Status,
                    reservation.CreatedAt
                })
                .ToListAsync();

        return Results.Ok(new
        {
            GeneratedAt = now,

            Counts = new
            {
                Customers = customerCount,
                Inquiries = inquiryCount,
                NewInquiries = newInquiryCount,

                Quotes = new
                {
                    Draft = draftQuoteCount,
                    Sent = sentQuoteCount,
                    Accepted = acceptedQuoteCount
                },

                Bookings = new
                {
                    Confirmed = confirmedBookingCount,
                    InPreparation =
                        inPreparationBookingCount
                },

                PendingReservations =
                    pendingReservationCount
            },

            RecentInquiries = recentInquiries,
            UpcomingBookings = upcomingBookings,
            QuotesNeedingAttention =
                quotesNeedingAttention,
            PendingReservations =
                pendingReservations
        });
    }
);

// =========================================================
// ADMIN USERS - LIST
// =========================================================

managerApi.MapGet(
    "/api/admin/users",
    async (AppDbContext db) =>
    {
        var users = await db.AppUsers
    .AsNoTracking()
    .OrderBy(user => user.FirstName)
    .ThenBy(user => user.LastName)
    .Select(user => new
    {
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Role,
        user.IsActive,
        user.ExternalId,
        user.CreatedAt,
        user.UpdatedAt,

        Positions = user.Positions
            .Select(p => p.Position)
            .OrderBy(p => p)
            .ToArray()
    })
    .ToListAsync();

        return Results.Ok(users);
    }
);

// =========================================================
// ADMIN USERS - CREATE
// =========================================================

managerApi.MapPost(
    "/api/admin/users",
    async (
        CreateAppUserRequest request,
        AppDbContext db
    ) =>
    {
        var email =
            request.Email?.Trim().ToLowerInvariant()
            ?? string.Empty;

        var firstName =
            request.FirstName?.Trim()
            ?? string.Empty;

        var lastName =
            request.LastName?.Trim()
            ?? string.Empty;

        var role =
            request.Role?.Trim()
            ?? string.Empty;

        if (
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName)
        )
        {
            return Results.BadRequest(new
            {
                Message =
                    "Email, first name, and last name are required."
            });
        }

        var allowedRoles = new[]
        {
            "Manager",
            "Employee"
        };

        var normalizedRole =
            allowedRoles.FirstOrDefault(
                allowedRole =>
                    allowedRole.Equals(
                        role,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        if (normalizedRole is null)
        {
            return Results.BadRequest(new
            {
                Message =
                    "Role must be Manager or Employee."
            });
        }

        var emailExists =
            await db.AppUsers.AnyAsync(user =>
                user.Email.ToLower() == email
            );

        if (emailExists)
        {
            return Results.Conflict(new
            {
                Message =
                    "A user with this email already exists."
            });
        }

        var user = new AppUser
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = normalizedRole,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.AppUsers.Add(user);

        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/users/{user.Id}",
            new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.IsActive,
                user.ExternalId,
                user.CreatedAt,
                user.UpdatedAt
            }
        );
    }
);

// =========================================================
// ADMIN USERS - UPDATE
// =========================================================

managerApi.MapPut(
    "/api/admin/users/{id:int}",
    async (
        int id,
        UpdateAppUserRequest request,
        AppDbContext db,
        HttpContext http
    ) =>
    {
        var user = await db.AppUsers
    .Include(u => u.Positions)
    .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return Results.NotFound(new
            {
                Message = "User not found."
            });
        }

        var email =
            request.Email?.Trim().ToLowerInvariant()
            ?? string.Empty;

        var firstName =
            request.FirstName?.Trim()
            ?? string.Empty;

        var lastName =
            request.LastName?.Trim()
            ?? string.Empty;

        if (
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName)
        )
        {
            return Results.BadRequest(new
            {
                Message =
                    "Email, first name, and last name are required."
            });
        }

        var allowedRoles = new[]
        {
            "Manager",
            "Employee"
        };

        var normalizedRole =
            allowedRoles.FirstOrDefault(
                allowedRole =>
                    allowedRole.Equals(
                        request.Role?.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        if (normalizedRole is null)
        {
            return Results.BadRequest(new
            {
                Message =
                    "Role must be Manager or Employee."
            });
        }
        // Prevent managers from accidentally demoting themselves.
var currentSubject = http.User.FindFirst("sub")?.Value;

if (
    !string.IsNullOrWhiteSpace(currentSubject) &&
    user.ExternalId == currentSubject &&
    user.Role == "Manager" &&
    normalizedRole != "Manager"
)
{
    return Results.BadRequest(new
    {
        Message = "You cannot remove your own Manager role."
    });
}
        var emailExists =
            await db.AppUsers.AnyAsync(existing =>
                existing.Id != id &&
                existing.Email.ToLower() == email
            );

        if (emailExists)
        {
            return Results.Conflict(new
            {
                Message =
                    "A user with this email already exists."
            });
        }

        // Null means the caller did not request a qualification change.
if (request.Positions is not null)
{
    var allowedPositions = new[]
    {
        "Barista",
        "Cashier",
        "Baker"
    };

    var positions = request.Positions
        .Select(p => p?.Trim() ?? string.Empty)
        .Where(p => p.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    var invalidPositions = positions
        .Where(p => !allowedPositions.Any(allowed =>
            allowed.Equals(p, StringComparison.OrdinalIgnoreCase)))
        .ToArray();

    if (invalidPositions.Length > 0)
    {
        return Results.BadRequest(new
        {
            Message = "Invalid employee qualification."
        });
    }

    // Normalize capitalization before saving.
    var normalizedPositions = positions
        .Select(p => allowedPositions.First(allowed =>
            allowed.Equals(p, StringComparison.OrdinalIgnoreCase)))
        .ToHashSet();

    // Remove qualifications that are no longer selected.
    var toRemove = user.Positions
        .Where(p => !normalizedPositions.Contains(p.Position))
        .ToList();

    db.EmployeePositions.RemoveRange(toRemove);

    // Add newly selected qualifications.
    foreach (var position in normalizedPositions)
    {
        if (!user.Positions.Any(p => p.Position == position))
        {
            user.Positions.Add(new EmployeePosition
            {
                Position = position,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}

user.Email = email;
user.FirstName = firstName;
user.LastName = lastName;
user.Role = normalizedRole;
user.IsActive = request.IsActive;
user.UpdatedAt = DateTimeOffset.UtcNow;

await db.SaveChangesAsync();

        return Results.Ok(new
{
    user.Id,
    user.Email,
    user.FirstName,
    user.LastName,
    user.Role,
    user.IsActive,
    user.ExternalId,
    user.CreatedAt,
    user.UpdatedAt,

    Positions = user.Positions
        .Where(p => db.Entry(p).State != EntityState.Deleted)
        .Select(p => p.Position)
        .OrderBy(p => p)
        .ToArray()
});
    }
);

// =========================================================
// ADMIN USERS - BULK IMPORT PREVIEW
// =========================================================

managerApi.MapPost(
    "/api/admin/users/import/preview",
    async (
        ImportEmployeesRequest request,
        AppDbContext db
    ) =>
    {
        if (request.Employees is null ||
            request.Employees.Count == 0 ||
            request.Employees.Count > 200)
        {
            return Results.BadRequest(new
            {
                Message = "Provide between 1 and 200 employees."
            });
        }

        var existingUsers = await db.AppUsers
            .AsNoTracking()
            .Select(user => new
            {
                user.Email,
                user.FirstName,
                user.LastName
            })
            .ToListAsync();

        var existingEmails = existingUsers
            .Select(user => user.Email.Trim().ToLowerInvariant())
            .ToHashSet();

        var seenEmails = new HashSet<string>();

        var allowedPositions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "Barista",
            "Cashier",
            "Baker"
        };

        var results = request.Employees.Select((employee, index) =>
        {
            var email = employee.Email?.Trim().ToLowerInvariant()
                ?? string.Empty;

            var firstName = employee.FirstName?.Trim()
                ?? string.Empty;

            var lastName = employee.LastName?.Trim()
                ?? string.Empty;

            var positions = (employee.Positions ?? Array.Empty<string>())
                .Select(position => position.Trim())
                .Where(position => position.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                errors.Add("First name, last name, and email are required.");
            }
if (!string.IsNullOrWhiteSpace(email))
{
    if (!System.Net.Mail.MailAddress.TryCreate(
            email, out var parsedAddress) ||
        !string.Equals(
            parsedAddress?.Address,
            email,
            StringComparison.OrdinalIgnoreCase))
    {
        errors.Add("Invalid email address.");
    }
}


            if (positions.Any(position => !allowedPositions.Contains(position)))
            {
                errors.Add("Unsupported position.");
            }

            if (!string.IsNullOrWhiteSpace(email) &&
                !seenEmails.Add(email))
            {
                errors.Add("Duplicate email within import.");
            }

            var isExisting = existingEmails.Contains(email);

            return new
            {
                Row = index + 1,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Positions = positions,
                Status = errors.Count > 0
                    ? "Invalid"
                    : isExisting
                        ? "Existing"
                        : "Ready",
                Errors = errors
            };
        }).ToList();

        return Results.Ok(new
        {
            Total = results.Count,
            Ready = results.Count(row => row.Status == "Ready"),
            Existing = results.Count(row => row.Status == "Existing"),
            Invalid = results.Count(row => row.Status == "Invalid"),
            Employees = results
        });
    }
);

// =========================================================
// ADMIN USERS - BULK IMPORT CONFIRM
// =========================================================

managerApi.MapPost(
    "/api/admin/users/import/confirm",
    async (
        ImportEmployeesRequest request,
        AppDbContext db
    ) =>
    {
        if (request.Employees is null ||
            request.Employees.Count == 0 ||
            request.Employees.Count > 200)
        {
            return Results.BadRequest(new
            {
                Message = "Provide between 1 and 200 employees."
            });
        }

        var allowedPositions = new[]
        {
            "Barista",
            "Cashier",
            "Baker"
        };

        var normalized = new List<(
            string FirstName,
            string LastName,
            string Email,
            string[] Positions
        )>();

        var seenEmails = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var employee in request.Employees)
        {
            if (employee is null)
            {
                return Results.BadRequest(new
                {
                    Message = "Employee rows cannot be null."
                });
            }

            var firstName = employee.FirstName?.Trim()
                ?? string.Empty;

            var lastName = employee.LastName?.Trim()
                ?? string.Empty;

            var email = employee.Email?.Trim().ToLowerInvariant()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                return Results.BadRequest(new
                {
                    Message = "Every employee needs a name and email."
                });
            }

            if (!System.Net.Mail.MailAddress.TryCreate(
                    email, out var parsedAddress) ||
                !string.Equals(
                    parsedAddress?.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new
                {
                    Message = $"Invalid email: {email}"
                });
            }

            if (!seenEmails.Add(email))
            {
                return Results.BadRequest(new
                {
                    Message = $"Duplicate email in import: {email}"
                });
            }

            var positions = new List<string>();

            foreach (var position in employee.Positions
                         ?? Array.Empty<string>())
            {
                var canonicalPosition = allowedPositions
                    .FirstOrDefault(allowed =>
                        allowed.Equals(
                            position?.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                if (canonicalPosition is null)
                {
                    return Results.BadRequest(new
                    {
                        Message = $"Unsupported position for {email}."
                    });
                }

                if (!positions.Contains(canonicalPosition))
                {
                    positions.Add(canonicalPosition);
                }
            }

            normalized.Add((
                firstName,
                lastName,
                email,
                positions.ToArray()
            ));
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable
            );

        var existingEmails = (await db.AppUsers
            .AsNoTracking()
            .Select(user => user.Email.ToLower())
            .ToListAsync())
            .ToHashSet();

        var imported = 0;
        var skipped = 0;

        foreach (var employee in normalized)
        {
            if (existingEmails.Contains(employee.Email))
            {
                skipped++;
                continue;
            }

            var user = new AppUser
            {
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Role = "Employee",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            foreach (var position in employee.Positions)
            {
                user.Positions.Add(new EmployeePosition
                {
                    Position = position,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            db.AppUsers.Add(user);
            imported++;
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Results.Ok(new
        {
            Imported = imported,
            SkippedExisting = skipped,
            Total = normalized.Count
        });
    }
);

// =========================================================
// COMMUNICATIONS - CREATE
// =========================================================

managerApi.MapPost(
    "/api/communications",
    async (
        CreateCommunicationRequest request,
        AppDbContext db
    ) =>
    {
        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer =>
                customer.Id == request.CustomerId
            );

        if (customer is null)
        {
            return Results.BadRequest(new
            {
                Message = "Customer not found."
            });
        }

        if (request.InquiryId.HasValue)
        {
            var inquiry = await db.Inquiries
                .AsNoTracking()
                .FirstOrDefaultAsync(inquiry =>
                    inquiry.Id == request.InquiryId.Value &&
                    inquiry.CustomerId == request.CustomerId
                );

            if (inquiry is null)
            {
                return Results.BadRequest(new
                {
                    Message =
                        "Inquiry does not belong to this customer."
                });
            }
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest(new
            {
                Message = "Message body is required."
            });
        }

        var communication = new Communication
        {
            CustomerId = request.CustomerId,
            InquiryId = request.InquiryId,
            Type = request.Type.Trim(),
            Direction = request.Direction.Trim(),
            Subject = request.Subject.Trim(),
            Body = request.Body.Trim(),
            FromAddress = request.FromAddress.Trim(),
            ToAddress = request.ToAddress.Trim(),
            Status = "Draft"
        };

        db.Communications.Add(communication);

        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/communications/{communication.Id}",
            communication
        );
    }
);

// =========================================================
// COMMUNICATIONS - GET ONE
// =========================================================

managerApi.MapGet(
    "/api/communications/{id:int}",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var communication = await db.Communications
            .AsNoTracking()
            .Where(communication =>
                communication.Id == id
            )
            .Select(communication => new
            {
                communication.Id,
                communication.CustomerId,
                communication.InquiryId,
                communication.Type,
                communication.Direction,
                communication.Subject,
                communication.Body,
                communication.FromAddress,
                communication.ToAddress,
                communication.Status,
                communication.CreatedAt,
                communication.SentAt,

                Customer = new
                {
                    communication.Customer.Id,
                    communication.Customer.FirstName,
                    communication.Customer.LastName,
                    communication.Customer.Company,
                    communication.Customer.Email
                }
            })
            .FirstOrDefaultAsync();

        return communication is null
            ? Results.NotFound(new
            {
                Message = "Communication not found."
            })
            : Results.Ok(communication);
    }
);

// =========================================================
// COMMUNICATIONS - GET BY CUSTOMER
// =========================================================

managerApi.MapGet(
    "/api/customers/{customerId:int}/communications",
    async (
        int customerId,
        AppDbContext db
    ) =>
    {
        var communications = await db.Communications
            .AsNoTracking()
            .Where(communication =>
                communication.CustomerId == customerId
            )
            .OrderByDescending(communication =>
                communication.CreatedAt
            )
            .Select(communication => new
            {
                communication.Id,
                communication.InquiryId,
                communication.Type,
                communication.Direction,
                communication.Subject,
                communication.Body,
                communication.FromAddress,
                communication.ToAddress,
                communication.Status,
                communication.CreatedAt,
                communication.SentAt
            })
            .ToListAsync();

        return Results.Ok(communications);
    }
);

// =========================================================
// EVENT COVER IMAGE API V1
// Manager uploads; public access only for published events.
// =========================================================

var eventImageDirectory =
    builder.Configuration["EventImages:Directory"]
    ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".reisters-daughter",
        "event-images");

managerApi.MapPost(
    "/api/events/{id:int}/cover-image",
    async (int id, HttpRequest request, AppDbContext db,
        CancellationToken cancellationToken) =>
    {
        const long maxImageBytes = 5L * 1024 * 1024;

        var eventItem = await db.Events.FindAsync(
            new object[] { id }, cancellationToken);

        if (eventItem is null)
            return Results.NotFound(new { Message = "Event not found." });

        if (!request.HasFormContentType)
            return Results.BadRequest(new {
                Message = "Multipart form data is required."
            });

        IFormCollection form;

        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (Exception ex) when (
            ex is InvalidDataException or BadHttpRequestException)
        {
            return Results.BadRequest(new {
                Message = "Invalid image upload."
            });
        }

        if (form.Files.Count != 1 ||
            form.Keys.Count != 0 ||
            form.Files[0].Name != "image")
        {
            return Results.BadRequest(new {
                Message = "Upload exactly one image in the 'image' field."
            });
        }

        var file = form.Files[0];

        if (file.Length == 0 || file.Length > maxImageBytes)
            return Results.BadRequest(new {
                Message = "Image must be between 1 byte and 5 MB."
            });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return Results.BadRequest(new {
                Message = "Only JPG, PNG and WebP images are supported."
            });

        var signature = new byte[12];

        await using (var stream = file.OpenReadStream())
        {
            var count = 0;

            while (count < signature.Length)
            {
                var read = await stream.ReadAsync(
                    signature.AsMemory(count),
                    cancellationToken);

                if (read == 0) break;
                count += read;
            }
        }

        bool jpeg =
            signature[0] == 0xFF &&
            signature[1] == 0xD8 &&
            signature[2] == 0xFF;

        bool png =
            signature.AsSpan(0, 8).SequenceEqual(
                new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        bool webp =
            System.Text.Encoding.ASCII.GetString(signature, 0, 4) == "RIFF" &&
            System.Text.Encoding.ASCII.GetString(signature, 8, 4) == "WEBP";

        var valid = extension switch
        {
            ".jpg" or ".jpeg" => jpeg,
            ".png" => png,
            ".webp" => webp,
            _ => false
        };

        if (!valid)
            return Results.BadRequest(new {
                Message = "File contents do not match the image format."
            });

        Directory.CreateDirectory(eventImageDirectory);

        var filename = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(eventImageDirectory, filename);

        try
        {
            await using var output = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await file.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            if (File.Exists(destination))
                File.Delete(destination);

            throw;
        }

        var previous = eventItem.CoverImagePath;
        eventItem.CoverImagePath = filename;
        eventItem.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            File.Delete(destination);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previous) &&
            previous != filename)
        {
            var oldFile = Path.Combine(
                eventImageDirectory, Path.GetFileName(previous));

            try { File.Delete(oldFile); }
            catch (IOException) { /* Keep orphan rather than fail update. */ }
        }

        return Results.Ok(new {
            eventItem.Id,
            eventItem.CoverImagePath,
            ImageUrl = $"/api/public/events/{id}/cover-image"
        });
    });


// EVENT ADMIN IMAGE PREVIEW V1
managerApi.MapGet(
    "/api/events/{id:int}/cover-image",
    async (int id, AppDbContext db) =>
    {
        var eventItem = await db.Events
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { e.CoverImagePath })
            .FirstOrDefaultAsync();

        if (eventItem?.CoverImagePath is not { Length: > 0 } filename)
            return Results.NotFound();

        var extension = Path.GetExtension(filename).ToLowerInvariant();

        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        if (contentType is null)
            return Results.NotFound();

        var imagePath = Path.Combine(
            eventImageDirectory,
            Path.GetFileName(filename));

        if (!File.Exists(imagePath))
            return Results.NotFound();

        return Results.File(imagePath, contentType);
    });

managerApi.MapDelete(
    "/api/events/{id:int}/cover-image",
    async (int id, AppDbContext db) =>
    {
        var eventItem = await db.Events.FindAsync(id);

        if (eventItem is null)
            return Results.NotFound(new { Message = "Event not found." });

        var previous = eventItem.CoverImagePath;

        eventItem.CoverImagePath = null;
        eventItem.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(previous))
        {
            var oldFile = Path.Combine(
                eventImageDirectory, Path.GetFileName(previous));

            try { File.Delete(oldFile); }
            catch (IOException) { /* Database removal still succeeded. */ }
        }

        return Results.NoContent();
    });

app.MapGet(
    "/api/public/events/{id:int}/cover-image",
    async (int id, AppDbContext db) =>
    {
        var eventItem = await db.Events
            .AsNoTracking()
            .Where(e =>
                e.Id == id &&
                e.IsPublic &&
                e.Status == "Published")
            .Select(e => new { e.CoverImagePath })
            .FirstOrDefaultAsync();

        if (eventItem?.CoverImagePath is not { Length: > 0 } filename)
            return Results.NotFound();

        var extension = Path.GetExtension(filename).ToLowerInvariant();

        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        if (contentType is null)
            return Results.NotFound();

        var path = Path.Combine(
            eventImageDirectory,
            Path.GetFileName(filename));

        if (!File.Exists(path))
            return Results.NotFound();

        return Results.File(
            path,
            contentType,
            enableRangeProcessing: false);
    });

// =========================================================
// EVENTS - GET ALL
// =========================================================

managerApi.MapGet(
    "/api/events",
    async (AppDbContext db) =>
    {
        var events = await db.Events
            .AsNoTracking()
            .OrderBy(eventItem => eventItem.StartDate)
            .ThenBy(eventItem => eventItem.StartTime)
            .Select(eventItem => new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Description,
                eventItem.Type,
                eventItem.Status,
                eventItem.StartDate,
                eventItem.StartTime,
                eventItem.EndDate,
                eventItem.EndTime,
                eventItem.Location,
                eventItem.IsPublic,
                eventItem.Capacity,
                eventItem.BookingId,
                eventItem.CreatedAt,
                eventItem.UpdatedAt
            })
            .ToListAsync();

        return Results.Ok(events);
    }
);

// =========================================================
// EVENTS - GET ONE
// =========================================================

managerApi.MapGet(
    "/api/events/{id:int}",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var eventItem = await db.Events
            .AsNoTracking()
            .Where(eventItem =>
                eventItem.Id == id
            )
            .Select(eventItem => new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Description,
                eventItem.Type,
                eventItem.Status,
                eventItem.StartDate,
                eventItem.StartTime,
                eventItem.EndDate,
                eventItem.EndTime,
                eventItem.Location,
                eventItem.IsPublic,
                eventItem.Capacity,
                eventItem.BookingId,
                eventItem.CreatedAt,
                eventItem.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return eventItem is null
            ? Results.NotFound(new
            {
                Message = "Event not found."
            })
            : Results.Ok(eventItem);
    }
);

// =========================================================
// EVENTS - CREATE
// =========================================================

managerApi.MapPost(
    "/api/events",
    async (
        CreateEventRequest request,
        AppDbContext db
    ) =>
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new
            {
                Message = "Event title is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return Results.BadRequest(new
            {
                Message = "Event type is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.StartDate))
        {
            return Results.BadRequest(new
            {
                Message = "Event start date is required."
            });
        }

        if (request.Capacity.HasValue &&
            request.Capacity.Value < 1)
        {
            return Results.BadRequest(new
            {
                Message =
                    "Capacity must be greater than zero."
            });
        }

        if (request.BookingId.HasValue)
        {
            var bookingExists = await db.Bookings
                .AsNoTracking()
                .AnyAsync(booking =>
                    booking.Id == request.BookingId.Value
                );

            if (!bookingExists)
            {
                return Results.BadRequest(new
                {
                    Message = "Booking not found."
                });
            }
        }

        var eventItem = new Event
        {
            Title = request.Title.Trim(),
            Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
            Type = request.Type.Trim(),
            Status = "Draft",
            StartDate = request.StartDate.Trim(),
            StartTime = request.StartTime.Trim(),
            EndDate =
                string.IsNullOrWhiteSpace(request.EndDate)
                    ? null
                    : request.EndDate.Trim(),
            EndTime =
                string.IsNullOrWhiteSpace(request.EndTime)
                    ? null
                    : request.EndTime.Trim(),
            Location =
                string.IsNullOrWhiteSpace(request.Location)
                    ? null
                    : request.Location.Trim(),
            IsPublic = request.IsPublic,
            Capacity = request.Capacity,
            BookingId = request.BookingId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Events.Add(eventItem);

        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/events/{eventItem.Id}",
            new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Description,
                eventItem.Type,
                eventItem.Status,
                eventItem.StartDate,
                eventItem.StartTime,
                eventItem.EndDate,
                eventItem.EndTime,
                eventItem.Location,
                eventItem.IsPublic,
                eventItem.Capacity,
                eventItem.BookingId,
                eventItem.CreatedAt,
                eventItem.UpdatedAt
            }
        );
    }
);

// =========================================================
// EVENTS - UPDATE
// =========================================================

managerApi.MapPut(
    "/api/events/{id:int}",
    async (
        int id,
        CreateEventRequest request,
        AppDbContext db
    ) =>
    {
        var eventItem = await db.Events
            .FirstOrDefaultAsync(eventItem =>
                eventItem.Id == id
            );

        if (eventItem is null)
        {
            return Results.NotFound(new
            {
                Message = "Event not found."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new
            {
                Message = "Event title is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return Results.BadRequest(new
            {
                Message = "Event type is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.StartDate))
        {
            return Results.BadRequest(new
            {
                Message = "Event start date is required."
            });
        }

        if (request.Capacity.HasValue &&
            request.Capacity.Value < 1)
        {
            return Results.BadRequest(new
            {
                Message =
                    "Capacity must be greater than zero."
            });
        }

        if (request.BookingId.HasValue)
        {
            var bookingExists = await db.Bookings
                .AsNoTracking()
                .AnyAsync(booking =>
                    booking.Id == request.BookingId.Value
                );

            if (!bookingExists)
            {
                return Results.BadRequest(new
                {
                    Message = "Booking not found."
                });
            }
        }

        eventItem.Title = request.Title.Trim();
        eventItem.Description =
            string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        eventItem.Type = request.Type.Trim();
        eventItem.StartDate = request.StartDate.Trim();
        eventItem.StartTime = request.StartTime.Trim();
        eventItem.EndDate =
            string.IsNullOrWhiteSpace(request.EndDate)
                ? null
                : request.EndDate.Trim();
        eventItem.EndTime =
            string.IsNullOrWhiteSpace(request.EndTime)
                ? null
                : request.EndTime.Trim();
        eventItem.Location =
            string.IsNullOrWhiteSpace(request.Location)
                ? null
                : request.Location.Trim();
        eventItem.IsPublic = request.IsPublic;
        eventItem.Capacity = request.Capacity;
        eventItem.BookingId = request.BookingId;
        eventItem.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            eventItem.Id,
            eventItem.Title,
            eventItem.Description,
            eventItem.Type,
            eventItem.Status,
            eventItem.StartDate,
            eventItem.StartTime,
            eventItem.EndDate,
            eventItem.EndTime,
            eventItem.Location,
            eventItem.IsPublic,
            eventItem.Capacity,
            eventItem.BookingId,
            eventItem.CreatedAt,
            eventItem.UpdatedAt
        });
    }
);

// =========================================================
// EVENTS - UPDATE STATUS
// =========================================================

managerApi.MapPut(
    "/api/events/{id:int}/status",
    async (
        int id,
        UpdateEventStatusRequest request,
        AppDbContext db
    ) =>
    {
        var allowedStatuses = new[]
        {
            "Draft",
            "Published",
            "Completed",
            "Cancelled"
        };

        var requestedStatus = request.Status.Trim();

        if (!allowedStatuses.Contains(
                requestedStatus,
                StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                Message =
                    "Status must be Draft, Published, Completed, or Cancelled."
            });
        }

        var normalizedStatus =
            allowedStatuses.First(status =>
                status.Equals(
                    requestedStatus,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        var eventItem = await db.Events
            .FirstOrDefaultAsync(eventItem =>
                eventItem.Id == id
            );

        if (eventItem is null)
        {
            return Results.NotFound(new
            {
                Message = "Event not found."
            });
        }

        eventItem.Status = normalizedStatus;
        eventItem.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            eventItem.Id,
            eventItem.Status,
            eventItem.UpdatedAt
        });
    }
);
// =========================================================
// EVENTS - GET RESERVATIONS
// =========================================================

managerApi.MapGet(
    "/api/events/{id:int}/reservations",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var eventItem = await db.Events
            .AsNoTracking()
            .Where(eventItem =>
                eventItem.Id == id
            )
            .Select(eventItem => new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Capacity
            })
            .FirstOrDefaultAsync();

        if (eventItem is null)
        {
            return Results.NotFound(new
            {
                Message = "Event not found."
            });
        }

        var reservations = await db.EventReservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.EventId == id
            )
            .OrderByDescending(reservation =>
                reservation.CreatedAt
            )
            .Select(reservation => new
            {
                reservation.Id,
                reservation.CustomerId,
                reservation.FirstName,
                reservation.LastName,
                reservation.Email,
                reservation.Phone,
                reservation.GuestCount,
                reservation.Status,
                reservation.Notes,
                reservation.CreatedAt,
                reservation.UpdatedAt
            })
            .ToListAsync();

        var reservedSeats = reservations
            .Where(reservation =>
                reservation.Status == "Pending" ||
                reservation.Status == "Confirmed"
            )
            .Sum(reservation =>
                reservation.GuestCount
            );

        var pendingReservations = reservations
            .Count(reservation =>
                reservation.Status == "Pending"
            );

        var confirmedReservations = reservations
            .Count(reservation =>
                reservation.Status == "Confirmed"
            );

        int? remainingSeats = eventItem.Capacity.HasValue
            ? Math.Max(
                eventItem.Capacity.Value - reservedSeats,
                0
            )
            : null;

        return Results.Ok(new
        {
            EventId = eventItem.Id,
            eventItem.Title,
            eventItem.Capacity,
            ReservedSeats = reservedSeats,
            RemainingSeats = remainingSeats,
            ReservationCount = reservations.Count,
            PendingReservations = pendingReservations,
            ConfirmedReservations = confirmedReservations,
            Reservations = reservations
        });
    }
);

// =========================================================
// EVENTS - UPDATE RESERVATION STATUS
// =========================================================

// =========================================================
// EVENTS - UPDATE RESERVATION STATUS
// =========================================================

managerApi.MapPut(
    "/api/events/{eventId:int}/reservations/{reservationId:int}/status",
    async (
        int eventId,
        int reservationId,
        UpdateEventReservationStatusRequest request,
        AppDbContext db
    ) =>
    {
        var allowedStatuses = new[]
        {
            "Pending",
            "Confirmed",
            "Cancelled"
        };

        var requestedStatus =
            request.Status?.Trim() ?? string.Empty;

        if (!allowedStatuses.Contains(
                requestedStatus,
                StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                Message =
                    "Status must be Pending, Confirmed, or Cancelled."
            });
        }

        var normalizedStatus =
            allowedStatuses.First(status =>
                status.Equals(
                    requestedStatus,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        var eventItem = await db.Events
            .FirstOrDefaultAsync(eventItem =>
                eventItem.Id == eventId
            );

        if (eventItem is null)
        {
            return Results.NotFound(new
            {
                Message = "Event not found."
            });
        }

        var reservation =
            await db.EventReservations
                .FirstOrDefaultAsync(reservation =>
                    reservation.Id == reservationId &&
                    reservation.EventId == eventId
                );

        if (reservation is null)
        {
            return Results.NotFound(new
            {
                Message = "Reservation not found."
            });
        }

        // A cancelled reservation no longer holds seats.
        // If it is restored, make sure capacity still exists.
        if (
            reservation.Status == "Cancelled" &&
            (
                normalizedStatus == "Pending" ||
                normalizedStatus == "Confirmed"
            ) &&
            eventItem.Capacity.HasValue
        )
        {
            var reservedSeats =
                await db.EventReservations
                    .Where(existing =>
                        existing.EventId == eventId &&
                        existing.Id != reservation.Id &&
                        (
                            existing.Status == "Pending" ||
                            existing.Status == "Confirmed"
                        )
                    )
                    .SumAsync(existing =>
                        existing.GuestCount
                    );

            var availableSeats =
                eventItem.Capacity.Value -
                reservedSeats;

            if (
                reservation.GuestCount >
                availableSeats
            )
            {
                return Results.BadRequest(new
                {
                    Message =
                        availableSeats > 0
                            ? $"This reservation needs {reservation.GuestCount} seats, but only {availableSeats} seat(s) remain."
                            : "This event is currently full.",
                    AvailableSeats =
                        Math.Max(
                            availableSeats,
                            0
                        )
                });
            }
        }

        reservation.Status = normalizedStatus;
        reservation.UpdatedAt =
            DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            reservation.Id,
            reservation.EventId,
            reservation.Status,
            reservation.GuestCount,
            reservation.UpdatedAt
        });
    }
);
// =========================================================
// EVENTS - DELETE RESERVATION
// =========================================================

managerApi.MapDelete(
    "/api/events/{eventId:int}/reservations/{reservationId:int}",
    async (
        int eventId,
        int reservationId,
        AppDbContext db
    ) =>
    {
        var reservation =
            await db.EventReservations
                .FirstOrDefaultAsync(reservation =>
                    reservation.Id == reservationId &&
                    reservation.EventId == eventId
                );

        if (reservation is null)
        {
            return Results.NotFound(new
            {
                Message = "Reservation not found."
            });
        }

        if (
            !reservation.Status.Equals(
                "Cancelled",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return Results.BadRequest(new
            {
                Message =
                    "Only cancelled reservations can be permanently deleted."
            });
        }

        db.EventReservations.Remove(
            reservation
        );

        await db.SaveChangesAsync();

        return Results.NoContent();
    }
);

// =========================================================
// PUBLIC EVENTS
// PUBLIC EVENT COVER DISPLAY V1
// Only events explicitly marked Public AND Published
// are exposed to the public website.
// =========================================================

app.MapGet(
    "/api/public/events",
    async (AppDbContext db) =>
    {
        var events = await db.Events
            .AsNoTracking()
            .Where(eventItem =>
                eventItem.IsPublic &&
                eventItem.Status == "Published"
            )
            .OrderBy(eventItem =>
                eventItem.StartDate
            )
            .ThenBy(eventItem =>
                eventItem.StartTime
            )
            .Select(eventItem => new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Description,
                eventItem.Type,
                eventItem.StartDate,
                eventItem.StartTime,
                eventItem.EndDate,
                eventItem.EndTime,
                eventItem.Location,
                eventItem.Capacity,
                eventItem.CoverImagePath
            })
            .ToListAsync();

        return Results.Ok(events);
    }
);
// =========================================================
// PUBLIC EVENT DETAIL
// Only Published + Public events can be viewed publicly.
// =========================================================

app.MapGet(
    "/api/public/events/{id:int}",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var eventItem = await db.Events
            .AsNoTracking()
            .Where(eventItem =>
                eventItem.Id == id &&
                eventItem.IsPublic &&
                eventItem.Status == "Published"
            )
            .Select(eventItem => new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Description,
                eventItem.Type,
                eventItem.StartDate,
                eventItem.StartTime,
                eventItem.EndDate,
                eventItem.EndTime,
                eventItem.Location,
                eventItem.Capacity,
                eventItem.CoverImagePath
            })
            .FirstOrDefaultAsync();

        if (eventItem is null)
        {
            return Results.NotFound(new
            {
                Message = "Event not found."
            });
        }

        return Results.Ok(eventItem);
    }
);

// =========================================================
// PUBLIC EVENT RESERVATIONS
// Creates a pending reservation for a Public + Published event.
// Capacity is enforced server-side.
// =========================================================

app.MapPost(
    "/api/public/events/{id:int}/reservations",
    async (
        int id,
        CreateEventReservationRequest request,
        AppDbContext db
    ) =>
    {
        var eventItem = await db.Events
            .FirstOrDefaultAsync(eventItem =>
                eventItem.Id == id &&
                eventItem.IsPublic &&
                eventItem.Status == "Published"
            );

        if (eventItem is null)
        {
            return Results.NotFound(new
            {
                Message = "Event not found."
            });
        }

        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var phone = request.Phone?.Trim() ?? string.Empty;
        var notes = request.Notes?.Trim();

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            return Results.BadRequest(new
            {
                Message = "First name and last name are required."
            });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Results.BadRequest(new
            {
                Message = "Email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            return Results.BadRequest(new
            {
                Message = "Phone number is required."
            });
        }

        if (request.GuestCount < 1)
        {
            return Results.BadRequest(new
            {
                Message = "Guest count must be at least 1."
            });
        }

        var reservedSeats = await db.EventReservations
            .Where(reservation =>
                reservation.EventId == id &&
                (
                    reservation.Status == "Pending" ||
                    reservation.Status == "Confirmed"
                )
            )
            .SumAsync(reservation => reservation.GuestCount);

        if (eventItem.Capacity.HasValue)
        {
            var availableSeats =
                eventItem.Capacity.Value - reservedSeats;

            if (request.GuestCount > availableSeats)
            {
                return Results.BadRequest(new
                {
                    Message = availableSeats > 0
                        ? $"Only {availableSeats} seat(s) remain."
                        : "This event is currently full.",
                    AvailableSeats = Math.Max(availableSeats, 0)
                });
            }
        }

        Customer? customer = null;

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.ToLower();

            customer = await db.Customers
                .FirstOrDefaultAsync(customer =>
                    customer.Email.ToLower() == normalizedEmail
                );
        }

        var reservation = new EventReservation
        {
            EventId = eventItem.Id,
            CustomerId = customer?.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            GuestCount = request.GuestCount,
            Status = "Pending",
            Notes = string.IsNullOrWhiteSpace(notes)
                ? null
                : notes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.EventReservations.Add(reservation);

        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/public/events/{id}/reservations/{reservation.Id}",
            new
            {
                reservation.Id,
                reservation.EventId,
                reservation.FirstName,
                reservation.LastName,
                reservation.Email,
                reservation.Phone,
                reservation.GuestCount,
                reservation.Status,
                reservation.CreatedAt
            }
        );
    }
);

// =========================================================
// MANAGER - WORK SCHEDULES
// =========================================================

// GET - Retrieve an existing schedule
managerApi.MapGet(
    "/api/admin/schedules",
    async (DateOnly startDate, AppDbContext db) =>
    {
        var firstStart = new DateOnly(2026, 11, 2);
        var offset = startDate.DayNumber - firstStart.DayNumber;

        if (offset < 0 || offset % 14 != 0)
        {
            return Results.BadRequest(
                "Invalid two-week scheduling period."
            );
        }

        var schedule = await db.WorkSchedules
            .AsNoTracking()
            .Where(s => s.StartDate == startDate)
            .Select(s => new
            {
                s.Id,
                s.StartDate,
                s.EndDate,
                Status = s.Status.ToString(),
                s.PublishedAt,
                s.CreatedAt,
                s.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (schedule is null)
        {
            return Results.NotFound(
                "No schedule exists for this period."
            );
        }

        var shifts = await db.WorkShifts
            .AsNoTracking()
            .Where(s => s.WorkScheduleId == schedule.Id)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.Date,
                s.StartTime,
                s.EndTime,
                s.Position,
                s.RequiredEmployees,
                Assignments = db.ShiftAssignments
                    .Where(a => a.WorkShiftId == s.Id)
                    .Select(a => new
                    {
                        a.AppUserId,
                        a.AppUser.FirstName,
                        a.AppUser.LastName
                    })
                    .ToList()
            })
            .ToListAsync();

        return Results.Ok(new
        {
            Schedule = schedule,
            Shifts = shifts
        });
    }
);


// POST - Create a new draft schedule
managerApi.MapPost(
    "/api/admin/schedules",
    async (DateOnly startDate, AppDbContext db) =>
    {
        var firstStart = new DateOnly(2026, 11, 2);
        var offset = startDate.DayNumber - firstStart.DayNumber;

        if (offset < 0 || offset % 14 != 0)
        {
            return Results.BadRequest(
                "Invalid two-week scheduling period."
            );
        }

        var existing = await db.WorkSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.StartDate == startDate);

        if (existing is not null)
        {
            return Results.Conflict(
                "A schedule already exists for this period."
            );
        }

        var schedule = new WorkSchedule
        {
            StartDate = startDate,
            EndDate = startDate.AddDays(13),
            Status = WorkScheduleStatus.Draft
        };

        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/schedules?startDate={startDate:yyyy-MM-dd}",
            new
            {
                schedule.Id,
                schedule.StartDate,
                schedule.EndDate,
                Status = schedule.Status.ToString()
            }
        );
    }
);
// =========================================================
// MANAGER - PUBLISH WORK SCHEDULE
// =========================================================

managerApi.MapPost(
    "/api/admin/schedules/{scheduleId:int}/publish",
    async (int scheduleId, AppDbContext db) =>
    {
        var schedule = await db.WorkSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule is null)
            return Results.NotFound("Schedule not found.");

        if (schedule.Status != WorkScheduleStatus.Draft)
            return Results.Conflict(
                "Only draft schedules can be published."
            );

        var shifts = await db.WorkShifts
            .AsNoTracking()
            .Where(s => s.WorkScheduleId == scheduleId)
            .Select(s => new
            {
                s.Id,
                s.RequiredEmployees,
                Assigned = db.ShiftAssignments.Count(a =>
                    a.WorkShiftId == s.Id)
            })
            .ToListAsync();

        if (shifts.Count == 0)
            return Results.BadRequest(
                "Add at least one shift before publishing."
            );

        var unfilled = shifts.Count(s =>
            s.Assigned < s.RequiredEmployees);

        if (unfilled > 0)
        {
            return Results.Conflict(
                $"{unfilled} shift(s) are not fully staffed. " +
                "Complete assignments before publishing."
            );
        }

        schedule.Status = WorkScheduleStatus.Published;
        schedule.PublishedAt = DateTimeOffset.UtcNow;
        schedule.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            schedule.Id,
            Status = schedule.Status.ToString(),
            schedule.PublishedAt
        });
    }
);
// =========================================================
// MANAGER - CREATE WORK SHIFT
// =========================================================

managerApi.MapPost(
    "/api/admin/schedules/{scheduleId:int}/shifts",
    async (
        int scheduleId,
        CreateWorkShiftInput input,
        AppDbContext db) =>
    {
        var schedule = await db.WorkSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule is null)
            return Results.NotFound("Schedule not found.");

        if (schedule.Status != WorkScheduleStatus.Draft)
            return Results.Conflict(
                "Published schedules cannot be edited."
            );

        if (input.Date < schedule.StartDate ||
            input.Date > schedule.EndDate)
        {
            return Results.BadRequest(
                "Shift date must fall within the schedule."
            );
        }

        if (input.EndTime <= input.StartTime)
        {
            return Results.BadRequest(
                "Shift end time must be after start time."
            );
        }

        if (input.RequiredEmployees < 1)
        {
            return Results.BadRequest(
                "At least one employee must be required."
            );
        }

        var position = input.Position?.Trim();

        if (string.IsNullOrWhiteSpace(position) ||
            position.Length > 100)
        {
            return Results.BadRequest(
                "Position must contain 1–100 characters."
            );
        }

        var shift = new WorkShift
        {
            WorkScheduleId = schedule.Id,
            Date = input.Date,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            Position = position,
            RequiredEmployees = input.RequiredEmployees
        };

        db.WorkShifts.Add(shift);

        schedule.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/schedules/{scheduleId}/shifts/{shift.Id}",
            new
            {
                shift.Id,
                shift.WorkScheduleId,
                shift.Date,
                shift.StartTime,
                shift.EndTime,
                shift.Position,
                shift.RequiredEmployees
            }
        );
    }
);
// =========================================================
// MANAGER - ASSIGN EMPLOYEE TO SHIFT
// =========================================================

managerApi.MapPost(
    "/api/admin/shifts/{shiftId:int}/assignments",
    async (
        int shiftId,
        AssignShiftInput input,
        AppDbContext db) =>
    {
        var shift = await db.WorkShifts
            .Include(s => s.WorkSchedule)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift is null)
            return Results.NotFound("Shift not found.");

        if (shift.WorkSchedule.Status != WorkScheduleStatus.Draft)
            return Results.Conflict(
                "Published schedules cannot be edited."
            );

        var employee = await db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == input.AppUserId &&
                u.IsActive &&
                u.Role == "Employee");

        if (employee is null)
            return Results.BadRequest(
                "Active employee not found."
            );

        var assignedCount = await db.ShiftAssignments
            .CountAsync(a => a.WorkShiftId == shiftId);

        if (assignedCount >= shift.RequiredEmployees)
            return Results.Conflict(
                "This shift is fully staffed."
            );

        var alreadyAssigned = await db.ShiftAssignments
            .AnyAsync(a =>
                a.WorkShiftId == shiftId &&
                a.AppUserId == employee.Id);

        if (alreadyAssigned)
            return Results.Conflict(
                "Employee is already assigned."
            );

        var overlapping = await db.ShiftAssignments
            .AnyAsync(a =>
                a.AppUserId == employee.Id &&
                a.WorkShift.Date == shift.Date &&
                a.WorkShift.StartTime < shift.EndTime &&
                a.WorkShift.EndTime > shift.StartTime);

        if (overlapping)
            return Results.Conflict(
                "Employee has an overlapping shift."
            );

        var availability = await db.AvailabilityEntries
            .AsNoTracking()
            .Where(e =>
                e.AppUserId == employee.Id &&
                e.AvailabilityPeriod.StartDate ==
                    shift.WorkSchedule.StartDate &&
                e.Date == shift.Date)
            .OrderBy(e => e.StartTime)
            .ToListAsync();

        var submissionExists = await db.AvailabilitySubmissions
            .AnyAsync(s =>
                s.AppUserId == employee.Id &&
                s.AvailabilityPeriod.StartDate ==
                    shift.WorkSchedule.StartDate);

        // Check continuous coverage, including adjacent windows.
        var coveredUntil = shift.StartTime;

        if (submissionExists)
        {
            foreach (var entry in availability)
            {
                if (entry.StartTime > coveredUntil)
                    break;

                if (entry.EndTime > coveredUntil)
                    coveredUntil = entry.EndTime;

                if (coveredUntil >= shift.EndTime)
                    break;
            }
        }

        var isAvailable =
            submissionExists &&
            coveredUntil >= shift.EndTime;

        if (!isAvailable && !input.OverrideAvailability)
        {
            return Results.Conflict(new
            {
                Code = "AvailabilityConflict",
                Message =
                    "Employee has not submitted availability " +
                    "covering this entire shift. " +
                    "An explicit manager override is required."
            });
        }

        db.ShiftAssignments.Add(new ShiftAssignment
        {
            WorkShiftId = shift.Id,
            AppUserId = employee.Id
        });

        shift.WorkSchedule.UpdatedAt =
            DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/shifts/{shiftId}/assignments",
            new
            {
                ShiftId = shift.Id,
                AppUserId = employee.Id,
                AvailabilityOverrideUsed =
                    !isAvailable && input.OverrideAvailability
            }
        );
    }
);

// =========================================================
// MANAGER - REMOVE SHIFT ASSIGNMENT
// =========================================================

managerApi.MapDelete(
    "/api/admin/shifts/{shiftId:int}/assignments/{employeeId:int}",
    async (int shiftId, int employeeId, AppDbContext db) =>
    {
        var shift = await db.WorkShifts
            .Include(s => s.WorkSchedule)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift is null)
            return Results.NotFound("Shift not found.");

        if (shift.WorkSchedule.Status != WorkScheduleStatus.Draft)
            return Results.Conflict("Schedule is published.");

        var assignment = await db.ShiftAssignments
            .FirstOrDefaultAsync(a =>
                a.WorkShiftId == shiftId &&
                a.AppUserId == employeeId);

        if (assignment is null)
            return Results.NotFound("Assignment not found.");

        db.ShiftAssignments.Remove(assignment);

        shift.WorkSchedule.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.NoContent();
    }
);


// =========================================================
// MANAGER - DELETE SHIFT
// =========================================================

managerApi.MapDelete(
    "/api/admin/shifts/{shiftId:int}",
    async (int shiftId, AppDbContext db) =>
    {
        var shift = await db.WorkShifts
            .Include(s => s.WorkSchedule)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift is null)
            return Results.NotFound("Shift not found.");

        if (shift.WorkSchedule.Status != WorkScheduleStatus.Draft)
            return Results.Conflict("Schedule is published.");

        db.WorkShifts.Remove(shift);

        shift.WorkSchedule.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.NoContent();
    }
);
// =========================================================
// MANAGER - EDIT WORK SHIFT
// =========================================================

managerApi.MapPut(
    "/api/admin/shifts/{shiftId:int}",
    async (
        int shiftId,
        CreateWorkShiftInput input,
        AppDbContext db) =>
    {
        var shift = await db.WorkShifts
            .Include(s => s.WorkSchedule)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift is null)
            return Results.NotFound("Shift not found.");

        if (shift.WorkSchedule.Status != WorkScheduleStatus.Draft)
            return Results.Conflict("Schedule is published.");

        if (input.Date < shift.WorkSchedule.StartDate ||
            input.Date > shift.WorkSchedule.EndDate)
            return Results.BadRequest("Date is outside the schedule.");

        if (input.EndTime <= input.StartTime)
            return Results.BadRequest("Invalid shift hours.");

        if (input.RequiredEmployees < 1)
            return Results.BadRequest("Invalid staffing requirement.");

        var position = input.Position?.Trim();

        if (string.IsNullOrWhiteSpace(position) ||
            position.Length > 100)
            return Results.BadRequest("Invalid position.");

        var assignments = await db.ShiftAssignments
            .Where(a => a.WorkShiftId == shiftId)
            .ToListAsync();

        if (assignments.Count > input.RequiredEmployees)
            return Results.Conflict(
                "Staffing requirement is below assigned count.");

        foreach (var assignment in assignments)
        {
            var overlapping = await db.ShiftAssignments
                .AnyAsync(a =>
                    a.AppUserId == assignment.AppUserId &&
                    a.WorkShiftId != shiftId &&
                    a.WorkShift.Date == input.Date &&
                    a.WorkShift.StartTime < input.EndTime &&
                    a.WorkShift.EndTime > input.StartTime);

            if (overlapping)
                return Results.Conflict(
                    $"Employee {assignment.AppUserId} has an overlapping shift.");

            var submitted = await db.AvailabilitySubmissions
                .AnyAsync(s =>
                    s.AppUserId == assignment.AppUserId &&
                    s.AvailabilityPeriod.StartDate ==
                        shift.WorkSchedule.StartDate);

            var windows = await db.AvailabilityEntries
                .AsNoTracking()
                .Where(e =>
                    e.AppUserId == assignment.AppUserId &&
                    e.AvailabilityPeriod.StartDate ==
                        shift.WorkSchedule.StartDate &&
                    e.Date == input.Date)
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            var coveredUntil = input.StartTime;

            if (submitted)
            {
                foreach (var window in windows)
                {
                    if (window.StartTime > coveredUntil)
                        break;

                    if (window.EndTime > coveredUntil)
                        coveredUntil = window.EndTime;

                    if (coveredUntil >= input.EndTime)
                        break;
                }
            }

            if (!submitted || coveredUntil < input.EndTime)
                return Results.Conflict(
                    $"Employee {assignment.AppUserId} is unavailable for the new hours. Remove or reassign them before editing.");
        }

        shift.Date = input.Date;
        shift.StartTime = input.StartTime;
        shift.EndTime = input.EndTime;
        shift.Position = position;
        shift.RequiredEmployees = input.RequiredEmployees;

        shift.WorkSchedule.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            shift.Id,
            shift.Date,
            shift.StartTime,
            shift.EndTime,
            shift.Position,
            shift.RequiredEmployees
        });
    }
);
// =========================================================
// START APPLICATION
// =========================================================

app.Run();


// =========================================================
// REQUEST MODELS
// These MUST remain after all top-level app statements.
// =========================================================

public record UpdateInquiryStatusRequest(
    string Status
);

public record CreateInquiryNoteRequest(
    string Body
);

public record CreateQuoteRequest(
    DateTimeOffset? ExpiresAt,
    string? CustomerMessage,
    decimal Tax,
    List<CreateQuoteItemRequest> Items
);

public record CreateQuoteItemRequest(
    string Description,
    int Quantity,
    decimal UnitPrice
);
public record UpdateQuoteRequest(
    DateTimeOffset? ExpiresAt,
    string? CustomerMessage,
    decimal Tax,
    List<CreateQuoteItemRequest> Items
);
public record UpdateBookingStatusRequest(
    string Status
);

public record UpdateBookingNotesRequest(
    string? InternalNotes
);
public record CreateCommunicationRequest(
    int CustomerId,
    int? InquiryId,
    string Type,
    string Direction,
    string Subject,
    string Body,
    string FromAddress,
    string ToAddress
);
public record CreateEventRequest(
    string Title,
    string? Description,
    string Type,
    string StartDate,
    string StartTime,
    string? EndDate,
    string? EndTime,
    string? Location,
    bool IsPublic,
    int? Capacity,
    int? BookingId
);
public record UpdateEventStatusRequest(
    string Status
);
public record CreateEventReservationRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int GuestCount,
    string? Notes
);
public record UpdateEventReservationStatusRequest(
    string Status
);
public record CreateAppUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Role
);

public record UpdateAppUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    string[]? Positions = null
);
public record AvailabilityInput(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime
);
public record CreateWorkShiftInput(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Position,
    int RequiredEmployees
);
public record BiweeklyAvailabilityEntryInput(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime
);

public record BiweeklyAvailabilityInput(
    DateOnly StartDate,
    List<BiweeklyAvailabilityEntryInput> Entries
);
public record AssignShiftInput(
    int AppUserId,
    bool OverrideAvailability = false
);
public record ImportEmployeeRow(
    string FirstName,
    string LastName,
    string Email,
    string[] Positions
);

public record ImportEmployeesRequest(
    List<ImportEmployeeRow> Employees
);
