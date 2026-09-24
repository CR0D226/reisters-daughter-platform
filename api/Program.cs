using Microsoft.EntityFrameworkCore;
using ReistersDaughter.Api.Data;
using ReistersDaughter.Api.Models;

var builder = WebApplication.CreateBuilder(args);


// DATABASE

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

app.UseCors("VueFrontend");


// API STATUS

app.MapGet("/", () =>
{
    return "The Reister's Daughter API is running!";
});


// MENU

app.MapGet("/api/menu", () =>
{
    return MenuData.GetMenuItems();
});


// CREATE INQUIRY

app.MapPost(
    "/api/inquiries",
    async (
        Inquiry inquiry,
        AppDbContext db
    ) =>
    {
        inquiry.Id = 0;
        inquiry.CreatedAt = DateTime.UtcNow;
        inquiry.Status = "New";

        db.Inquiries.Add(inquiry);

        await db.SaveChangesAsync();

        Console.WriteLine(
            $"New inquiry #{inquiry.Id}: " +
            $"{inquiry.FirstName} {inquiry.LastName}"
        );

        return Results.Created(
            $"/api/inquiries/{inquiry.Id}",
            new
            {
                inquiry.Id,
                inquiry.Status,
                inquiry.CreatedAt,
                message = "Inquiry received successfully."
            }
        );
    }
);


// GET ALL INQUIRIES

app.MapGet(
    "/api/inquiries",
    async (AppDbContext db) =>
    {
        var inquiries =
            await db.Inquiries
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

        return Results.Ok(inquiries);
    }
);


// GET ONE INQUIRY
// GET ONE INQUIRY

app.MapGet(
    "/api/inquiries/{id:int}",
    async (
        int id,
        AppDbContext db
    ) =>
    {
        var inquiry =
            await db.Inquiries.FindAsync(id);

        if (inquiry is null)
        {
            return Results.NotFound(new
            {
                message = "Inquiry not found."
            });
        }

        return Results.Ok(inquiry);
    }
);

// UPDATE INQUIRY STATUS

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
                message = "Invalid inquiry status."
            });
        }

        var inquiry =
            await db.Inquiries.FindAsync(id);

        if (inquiry is null)
        {
            return Results.NotFound(new
            {
                message = "Inquiry not found."
            });
        }

        inquiry.Status = request.Status;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            inquiry.Id,
            inquiry.Status
        });
    }
);


app.Run();


// IMPORTANT:
// Type declarations must come AFTER all top-level statements.

public record UpdateInquiryStatusRequest(
    string Status
);