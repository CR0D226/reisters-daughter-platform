using Microsoft.EntityFrameworkCore;
using ReistersDaughter.Api.Models;

namespace ReistersDaughter.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InquiryNote> InquiryNotes => Set<InquiryNote>();
    public DbSet<InquiryActivity> InquiryActivities => Set<InquiryActivity>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Communication> Communications => Set<Communication>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventReservation> EventReservations => Set<EventReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.Property(quote => quote.Subtotal)
                .HasPrecision(12, 2);

            entity.Property(quote => quote.Tax)
                .HasPrecision(12, 2);

            entity.Property(quote => quote.Total)
                .HasPrecision(12, 2);

            entity.HasIndex(quote => quote.PublicToken)
                .IsUnique();
        });

        modelBuilder.Entity<QuoteItem>(entity =>
        {
            entity.Property(item => item.UnitPrice)
                .HasPrecision(12, 2);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(booking => booking.Total)
                .HasPrecision(12, 2);

            entity.HasIndex(booking => booking.QuoteId)
                .IsUnique();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(eventItem => eventItem.Booking)
                .WithMany()
                .HasForeignKey(eventItem => eventItem.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(eventItem => eventItem.BookingId);

            entity.HasIndex(eventItem => new
            {
                eventItem.StartDate,
                eventItem.StartTime,
            });
        });

        modelBuilder.Entity<EventReservation>(entity =>
        {
            entity.HasOne(reservation => reservation.Event)
                .WithMany()
                .HasForeignKey(reservation => reservation.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(reservation => reservation.Customer)
                .WithMany()
                .HasForeignKey(reservation => reservation.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(reservation => reservation.EventId);

            entity.HasIndex(reservation => reservation.CustomerId);

            entity.HasIndex(reservation => new
            {
                reservation.EventId,
                reservation.Status,
            });

            entity.HasIndex(reservation => reservation.Email);
        });
    }
}