using Sanctum.Models;

namespace Sanctum.Data;

using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Room> Rooms { get; set; }

    public DbSet<Room> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Booking>()
            .HasOne(u => u.User)
            .WithMany(b => b.Bookings)
            .HasForeignKey(b => b.UserID)
            .OnDelete(DeleteBehavior.Cascade);
    }

}