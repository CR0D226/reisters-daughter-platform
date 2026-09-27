using Microsoft.EntityFrameworkCore;
using ReistersDaughter.Api.Data;
using ReistersDaughter.Api.Models;

var builder = WebApplication.CreateBuilder(args);


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
                "http://localhost:5174"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

app.UseCors("VueDev");


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
// MENU
// =========================================================

app.MapGet("/api/menu", () =>
{
    return Results.Ok(MenuData.GetMenuItems());
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
                    customer.Email.ToLower() == normalizedEmail
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

app.MapGet(
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

app.MapGet(
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
// INQUIRIES - UPDATE STATUS
// =========================================================

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

app.MapPost(
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

app.MapGet(
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
app.MapPut(
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

app.MapPost(
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

app.MapGet(
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

        if (quote.Status == "Accepted")
        {
            return Results.Ok(new
            {
                quote.QuoteNumber,
                quote.Status,
                Message = "Quote has already been accepted."
            });
        }

        if (quote.Status != "Sent")
        {
            return Results.BadRequest(new
            {
                Message = "Only sent quotes can be accepted."
            });
        }

        if (
            quote.ExpiresAt.HasValue &&
            quote.ExpiresAt.Value < DateTimeOffset.UtcNow
        )
        {
            return Results.BadRequest(new
            {
                Message = "This quote has expired."
            });
        }

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

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            quote.QuoteNumber,
            quote.Status,
            quote.UpdatedAt,
            Message = "Quote accepted successfully."
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