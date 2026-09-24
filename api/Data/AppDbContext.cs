using Microsoft.EntityFrameworkCore;
using ReistersDaughter.Api.Models;

namespace ReistersDaughter.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
}