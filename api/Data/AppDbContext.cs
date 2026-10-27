
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
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<EmployeePosition> EmployeePositions =>
    Set<EmployeePosition>();
    public DbSet<EmployeeAvailability> EmployeeAvailabilities
        => Set<EmployeeAvailability>();
    public DbSet<AvailabilityPeriod> AvailabilityPeriods
    => Set<AvailabilityPeriod>();

public DbSet<AvailabilityEntry> AvailabilityEntries
    => Set<AvailabilityEntry>();

public DbSet<AvailabilitySubmission> AvailabilitySubmissions
    => Set<AvailabilitySubmission>();
    public DbSet<WorkSchedule> WorkSchedules
    => Set<WorkSchedule>();

public DbSet<WorkShift> WorkShifts
    => Set<WorkShift>();

public DbSet<ShiftAssignment> ShiftAssignments
    => Set<ShiftAssignment>();
public DbSet<TimeOffRequest> TimeOffRequests
    => Set<TimeOffRequest>();

    // JOB APPLICATIONS
    public DbSet<JobApplication> JobApplications
        => Set<JobApplication>();

    public DbSet<JobApplicationNote> JobApplicationNotes
        => Set<JobApplicationNote>();

    public DbSet<JobApplicationActivity> JobApplicationActivities
        => Set<JobApplicationActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =====================================================
        // JOB APPLICATIONS
        // =====================================================

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.Property(a => a.Position)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(a => a.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.Email)
                .IsRequired()
                .HasMaxLength(254);

            entity.Property(a => a.Phone)
                .HasMaxLength(30);

            entity.Property(a => a.DesiredWeeklyHours)
                .HasMaxLength(100);

            entity.Property(a => a.ResumeOriginalFileName)
                .HasMaxLength(255);

            entity.Property(a => a.ResumeStorageKey)
                .HasMaxLength(500);

            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.Position);
        });

        modelBuilder.Entity<JobApplicationNote>(entity =>
        {
            entity.HasOne(n => n.JobApplication)
                .WithMany(a => a.Notes)
                .HasForeignKey(n => n.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(n => n.Body)
                .IsRequired()
                .HasMaxLength(5000);
        });

        modelBuilder.Entity<JobApplicationActivity>(entity =>
        {
            entity.HasOne(a => a.JobApplication)
                .WithMany(j => j.Activities)
                .HasForeignKey(a => a.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(a => a.Type)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.Description)
                .HasMaxLength(2000);
        });


        // =====================================================
        // QUOTES
        // =====================================================

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
        }
        );

        // =====================================================
        // BOOKINGS
        // =====================================================

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(booking => booking.Total)
                .HasPrecision(12, 2);

            entity.HasIndex(booking => booking.QuoteId)
                .IsUnique();
        });

        // =====================================================
        // EVENTS
        // =====================================================

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

        // =====================================================
        // APP USERS
        // =====================================================

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.HasIndex(user => user.ExternalId)
                .IsUnique();

            entity.HasIndex(user => new
            {
                user.Role,
                user.IsActive,
            });
        });

        // =====================================================
        // EMPLOYEE AVAILABILITY
        // =====================================================

        modelBuilder.Entity<EmployeeAvailability>(entity =>
        {
            entity.HasOne(availability => availability.AppUser)
                .WithMany()
                .HasForeignKey(availability => availability.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(availability => availability.DayOfWeek)
                .HasConversion<int>();

            entity.HasIndex(availability => new
            {
                availability.AppUserId,
                availability.DayOfWeek,
                availability.StartTime,
            })
            .IsUnique();

            entity.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_EmployeeAvailability_TimeRange",
                    "\"EndTime\" > \"StartTime\""
                )
            );
        });
        modelBuilder.Entity<AvailabilityPeriod>(entity =>
{
    entity.HasIndex(p => p.StartDate).IsUnique();

    entity.ToTable(table =>
        table.HasCheckConstraint(
            "CK_AvailabilityPeriod_Dates",
            "\"EndDate\" = \"StartDate\" + 13"
        ));
});

modelBuilder.Entity<AvailabilityEntry>(entity =>
{
    entity.HasOne(e => e.AvailabilityPeriod)
        .WithMany()
        .HasForeignKey(e => e.AvailabilityPeriodId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(e => e.AppUser)
        .WithMany()
        .HasForeignKey(e => e.AppUserId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(e => new
    {
        e.AvailabilityPeriodId,
        e.AppUserId,
        e.Date,
        e.StartTime
    }).IsUnique();

    entity.ToTable(table =>
        table.HasCheckConstraint(
            "CK_AvailabilityEntry_TimeRange",
            "\"EndTime\" > \"StartTime\""
        ));
});

modelBuilder.Entity<AvailabilitySubmission>(entity =>
{
    entity.HasOne(s => s.AvailabilityPeriod)
        .WithMany()
        .HasForeignKey(s => s.AvailabilityPeriodId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(s => s.AppUser)
        .WithMany()
        .HasForeignKey(s => s.AppUserId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.Property(s => s.Status)
        .HasConversion<string>();

    entity.HasIndex(s => new
    {
        s.AvailabilityPeriodId,
        s.AppUserId
    }).IsUnique();

modelBuilder.Entity<EmployeePosition>(entity =>
{
    entity.HasKey(position => position.Id);

    entity.Property(position => position.Position)
        .IsRequired()
        .HasMaxLength(50);

    entity.HasIndex(position => new
    {
        position.AppUserId,
        position.Position
    }).IsUnique();

    entity.HasOne(position => position.AppUser)
        .WithMany(user => user.Positions)
        .HasForeignKey(position => position.AppUserId)
        .OnDelete(DeleteBehavior.Cascade);
});


modelBuilder.Entity<TimeOffRequest>(entity =>
{
    entity.Property(request => request.Status)
        .HasConversion<string>();

    entity.Property(request => request.Reason)
        .HasMaxLength(1000);

    entity.Property(request => request.ManagerNote)
        .HasMaxLength(1000);

    entity.HasOne(request => request.AppUser)
        .WithMany()
        .HasForeignKey(request => request.AppUserId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(request => request.ReviewedBy)
        .WithMany()
        .HasForeignKey(request => request.ReviewedByAppUserId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(request => new
    {
        request.AppUserId,
        request.StartDate,
        request.EndDate
    });

    entity.ToTable(table =>
        table.HasCheckConstraint(
            "CK_TimeOffRequest_Dates",
            "\"EndDate\" >= \"StartDate\""
        ));
});

});
// =====================================================
// WORK SCHEDULING
// =====================================================

modelBuilder.Entity<WorkSchedule>(entity =>
{
    entity.Property(s => s.Status)
        .HasConversion<string>();

    entity.HasIndex(s => s.StartDate)
        .IsUnique();

    entity.ToTable(table =>
        table.HasCheckConstraint(
            "CK_WorkSchedule_Dates",
            "\"EndDate\" = \"StartDate\" + 13"
        ));
});

modelBuilder.Entity<WorkShift>(entity =>
{
    entity.HasOne(s => s.WorkSchedule)
        .WithMany()
        .HasForeignKey(s => s.WorkScheduleId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(s => new
    {
        s.WorkScheduleId,
        s.Date
    });

    entity.ToTable(table =>
    {
        table.HasCheckConstraint(
            "CK_WorkShift_TimeRange",
            "\"EndTime\" > \"StartTime\""
        );

        table.HasCheckConstraint(
            "CK_WorkShift_RequiredEmployees",
            "\"RequiredEmployees\" > 0"
        );
    });
});

modelBuilder.Entity<ShiftAssignment>(entity =>
{
    entity.HasOne(a => a.WorkShift)
        .WithMany()
        .HasForeignKey(a => a.WorkShiftId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(a => a.AppUser)
        .WithMany()
        .HasForeignKey(a => a.AppUserId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(a => new
    {
        a.WorkShiftId,
        a.AppUserId
    }).IsUnique();
});
    }
}
