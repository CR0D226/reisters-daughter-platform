using Microsoft.EntityFrameworkCore;
using ReistersDaughter.Api.Data;
using ReistersDaughter.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Allow the Vue development server to call the API.
// 5174 is included because Vite has also been running there.
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("VueDev");


// ---------------------------------------------------------
// BASIC API
// ---------------------------------------------------------

app.MapGet("/", () => new
{
    Message = "The Reister's Daughter API is running."
});


// ---------------------------------------------------------
// MENU
// ---------------------------------------------------------

app.MapGet("/api/menu", () =>
{
    return Results.Ok(MenuData.GetMenuItems());
});


// ---------------------------------------------------------
// INQUIRIES - CREATE
// ---------------------------------------------------------

app.MapPost("/api/inquiries", async (Inquiry inquiry, AppDbContext db) =>
{
    inquiry.Id = 0;
    inquiry.CreatedAt = DateTime.UtcNow;
    inquiry.Status = "New";

    var normalizedEmail = inquiry.Email
        .Trim()
        .ToLowerInvariant();

    var customer = await db.Customers
        .FirstOrDefaultAsync(c =>
            c.Email.ToLower() == normalizedEmail);

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
        // Keep the customer record current while the inquiry
        // retains its own contact-information snapshot.
        customer.FirstName = inquiry.FirstName.Trim();
        customer.LastName = inquiry.LastName.Trim();
        customer.Company = inquiry.Company?.Trim();
        customer.Phone = inquiry.Phone.Trim();
    }

    inquiry.Customer = customer;

    db.Inquiries.Add(inquiry);

    // Save first so PostgreSQL generates the Inquiry ID.
    await db.SaveChangesAsync();

    db.InquiryActivities.Add(new InquiryActivity
    {
        InquiryId = inquiry.Id,
        Type = "InquiryCreated",
        Description = "Inquiry received."
    });

    await db.SaveChangesAsync();

    Console.WriteLine(
        $"New inquiry #{inquiry.Id} from customer #{customer.Id}: " +
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
});


// ---------------------------------------------------------
// INQUIRIES - LIST
// ---------------------------------------------------------

app.MapGet("/api/inquiries", async (AppDbContext db) =>
{
    var inquiries = await db.Inquiries
        .AsNoTracking()
        .OrderByDescending(i => i.CreatedAt)
        .ToListAsync();

    return Results.Ok(inquiries);
});


// ---------------------------------------------------------
// INQUIRIES - DETAIL
// ---------------------------------------------------------

app.MapGet("/api/inquiries/{id:int}", async (int id, AppDbContext db) =>
{
    var inquiry = await db.Inquiries
        .AsNoTracking()
        .Where(i => i.Id == id)
        .Select(i => new
        {
            i.Id,
            i.CustomerId,
            i.CreatedAt,
            i.Status,

            i.EventType,
            i.Services,

            i.FirstName,
            i.LastName,
            i.Company,
            i.Email,
            i.Phone,

            i.EventDate,
            i.EventTime,
            i.GuestCount,

            i.CateringType,
            i.ServiceType,
            i.DeliveryAddress,
            i.Packaging,

            i.DietaryNeeds,
            i.OtherDietaryNeeds,
            i.Recurring,

            i.Details,

            Customer = i.Customer == null
                ? null
                : new
                {
                    i.Customer.Id,
                    i.Customer.FirstName,
                    i.Customer.LastName,
                    i.Customer.Company,
                    i.Customer.Email,
                    i.Customer.Phone,
                    i.Customer.CreatedAt
                },

            Notes = i.Notes
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Body,
                    n.CreatedAt
                })
                .ToList(),

            Activities = i.Activities
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Type,
                    a.Description,
                    a.CreatedAt
                })
                .ToList()
        })
        .FirstOrDefaultAsync();

    return inquiry is null
        ? Results.NotFound()
        : Results.Ok(inquiry);
});


// ---------------------------------------------------------
// INQUIRIES - UPDATE STATUS
// ---------------------------------------------------------

app.MapPatch(
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

        // Don't create duplicate activity when the status
        // hasn't actually changed.
        if (previousStatus == request.Status)
        {
            return Results.Ok(new
            {
                inquiry.Id,
                inquiry.Status
            });
        }

        inquiry.Status = request.Status;

        db.InquiryActivities.Add(new InquiryActivity
        {
            InquiryId = inquiry.Id,
            Type = "StatusChanged",
            Description =
                $"Status changed from {previousStatus} to {request.Status}."
        });

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            inquiry.Id,
            inquiry.Status
        });
    }
);


// ---------------------------------------------------------
// INQUIRIES - INTERNAL NOTES
// ---------------------------------------------------------

app.MapPost(
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

        db.InquiryActivities.Add(new InquiryActivity
        {
            InquiryId = inquiry.Id,
            Type = "NoteAdded",
            Description = "Internal note added."
        });

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


app.Run();


// ---------------------------------------------------------
// REQUEST MODELS
// ---------------------------------------------------------

public record UpdateInquiryStatusRequest(string Status);

public record CreateInquiryNoteRequest(string Body);