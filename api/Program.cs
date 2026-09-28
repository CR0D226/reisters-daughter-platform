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
// INQUIRIES - LINK CUSTOMER
// =========================================================

app.MapPost(
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

app.MapGet(
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

app.MapGet(
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

app.MapPatch(
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

app.MapPatch(
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

app.MapGet(
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

app.MapGet(
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

app.MapGet(
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
                }
            },

            RecentInquiries = recentInquiries,
            UpcomingBookings = upcomingBookings,
            QuotesNeedingAttention =
                quotesNeedingAttention
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