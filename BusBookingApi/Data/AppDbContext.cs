using BusBookingApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusBookingApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<BusSeat> BusSeats => Set<BusSeat>();
    public DbSet<BusRoute> BusRoutes => Set<BusRoute>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingLeg> BookingLegs => Set<BookingLeg>();
    public DbSet<SeatReservation> SeatReservations
        => Set<SeatReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Stop>()
            .Property(s => s.Name)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<Stop>()
            .Property(s => s.City)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Bus>()
            .HasIndex(b => b.BusNumber)
            .IsUnique();

        modelBuilder.Entity<BusSeat>()
            .HasIndex(s => new { s.BusId, s.SeatNumber })
            .IsUnique();

        modelBuilder.Entity<RouteStop>()
            .HasIndex(rs => new { rs.BusRouteId, rs.StopOrder })
            .IsUnique();

        modelBuilder.Entity<BusRoute>()
            .Property(r => r.Name)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingReference)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Trip>()
            .Property(t => t.BaseFarePerKm)
            .HasPrecision(10, 2);

        modelBuilder.Entity<BookingLeg>()
            .Property(bl => bl.Fare)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RouteStop>()
            .Property(rs => rs.DistanceFromStartKm)
            .HasPrecision(10, 2);

        modelBuilder.Entity<BusRoute>()
            .HasMany(r => r.RouteStops)
            .WithOne(rs => rs.BusRoute)
            .HasForeignKey(rs => rs.BusRouteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RouteStop>()
            .HasOne(rs => rs.Stop)
            .WithMany(s => s.RouteStops)
            .HasForeignKey(rs => rs.StopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Bus)
            .WithMany(b => b.Trips)
            .HasForeignKey(t => t.BusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.BusRoute)
            .WithMany(r => r.Trips)
            .HasForeignKey(t => t.BusRouteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookingLeg>()
            .HasOne(bl => bl.FromStop)
            .WithMany()
            .HasForeignKey(bl => bl.FromStopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookingLeg>()
            .HasOne(bl => bl.ToStop)
            .WithMany()
            .HasForeignKey(bl => bl.ToStopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SeatReservation>()
            .HasOne(sr => sr.BusSeat)
            .WithMany()
            .HasForeignKey(sr => sr.BusSeatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}