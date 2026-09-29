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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.Property(q => q.Subtotal)
                .HasPrecision(12, 2);

            entity.Property(q => q.Tax)
                .HasPrecision(12, 2);

            entity.Property(q => q.Total)
                .HasPrecision(12, 2);
            entity
        .HasIndex(q => q.PublicToken)
                        .IsUnique();
        });

        modelBuilder.Entity<QuoteItem>(entity =>
        {
            entity.Property(i => i.UnitPrice)
                .HasPrecision(12, 2);
        });
        modelBuilder.Entity<Booking>(entity =>
{
    entity.Property(booking => booking.Total)
        .HasPrecision(12, 2);

    entity.HasIndex(booking => booking.QuoteId)
        .IsUnique();
});
    }
}