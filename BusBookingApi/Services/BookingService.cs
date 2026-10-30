using System.Data;
using BusBookingApi.Data;
using BusBookingApi.DTOs;
using BusBookingApi.Entities;
using BusBookingApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BusBookingApi.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BookingResponseDto> CreateAsync(
        int userId,
        CreateBookingDto dto)
    {
        if (dto.Legs == null ||
            dto.Legs.Count < 1 ||
            dto.Legs.Count > 2)
        {
            throw new ArgumentException(
                "Provide one journey leg or two for a return trip.");
        }

        // Serialize booking checks to reduce simultaneous seat conflicts.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var booking = new Booking
        {
            UserId = userId,
            BookingReference =
                "BUS-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            Status = "Confirmed",
            CreatedAtUtc = DateTime.UtcNow
        };

        var requestedSeats = new List<(
            int TripId,
            int SeatId,
            int FromOrder,
            int ToOrder)>();

        // Acquire locks in a consistent order to reduce deadlock risk.
        foreach (var item in dto.Legs
            .Select(x => new { x.TripId, SeatId = x.BusSeatId })
            .Distinct()
            .OrderBy(x => x.TripId)
            .ThenBy(x => x.SeatId))
        {
            var resource = $"bus-seat:{item.TripId}:{item.SeatId}";

            await _db.Database.ExecuteSqlInterpolatedAsync($@"
        DECLARE @lockResult int;

        EXEC @lockResult = sys.sp_getapplock
            @Resource = {resource},
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @lockResult < 0
            THROW 50001, 'Could not acquire seat reservation lock.', 1;
    ");
        }

        foreach (var item in dto.Legs)
        {
            if (item.FromStopId == item.ToStopId)
            {
                throw new ArgumentException(
                    "Origin and destination must be different.");
            }

            var trip = await _db.Trips
                .Include(t => t.Bus)
                .Include(t => t.BusRoute)
                    .ThenInclude(r => r.RouteStops)
                        .ThenInclude(rs => rs.Stop)
                .FirstOrDefaultAsync(t => t.Id == item.TripId);

            if (trip == null)
            {
                throw new ArgumentException("Trip not found.");
            }

            var from = trip.BusRoute.RouteStops
                .FirstOrDefault(rs => rs.StopId == item.FromStopId);

            var to = trip.BusRoute.RouteStops
                .FirstOrDefault(rs => rs.StopId == item.ToStopId);

            if (from == null || to == null ||
                from.StopOrder >= to.StopOrder)
            {
                throw new ArgumentException(
                    "The selected stops are not in the correct order on this route.");
            }

            var seat = await _db.BusSeats
                .FirstOrDefaultAsync(s =>
                    s.Id == item.BusSeatId &&
                    s.BusId == trip.BusId);

            if (seat == null)
            {
                throw new ArgumentException(
                    "The selected seat does not belong to this bus.");
            }

            // Same seat, same trip, overlapping route segments = conflict.
            var alreadyBooked = await _db.SeatReservations
                .AnyAsync(sr =>
                    sr.BusSeatId == item.BusSeatId &&
                    sr.Status == "Confirmed" &&
                    sr.FromStopOrder < to.StopOrder &&
                    sr.ToStopOrder > from.StopOrder &&
                    sr.BookingLeg.TripId == trip.Id &&
                    sr.BookingLeg.Booking.Status == "Confirmed");

            if (alreadyBooked)
            {
                throw new ArgumentException(
                    $"Seat {seat.SeatNumber} is no longer available.");
            }

            // Also prevent overlapping seats within this request itself.
            var duplicateInRequest = requestedSeats.Any(x =>
                x.TripId == trip.Id &&
                x.SeatId == item.BusSeatId &&
                x.FromOrder < to.StopOrder &&
                x.ToOrder > from.StopOrder);

            if (duplicateInRequest)
            {
                throw new ArgumentException(
                    "The same seat was selected for overlapping journeys.");
            }

            requestedSeats.Add((
                trip.Id,
                item.BusSeatId,
                from.StopOrder,
                to.StopOrder));

            var distance =
                to.DistanceFromStartKm - from.DistanceFromStartKm;

            if (distance <= 0)
            {
                throw new ArgumentException(
                    "Invalid route distance configuration.");
            }

            var fare = distance * trip.BaseFarePerKm;

            var leg = new BookingLeg
            {
                TripId = trip.Id,
                Trip = trip,
                FromStopId = from.StopId,
                FromStop = from.Stop,
                ToStopId = to.StopId,
                ToStop = to.Stop,
                FromStopOrder = from.StopOrder,
                ToStopOrder = to.StopOrder,
                Fare = fare
            };

            leg.SeatReservations.Add(new SeatReservation
            {
                BusSeatId = seat.Id,
                BusSeat = seat,
                FromStopOrder = from.StopOrder,
                ToStopOrder = to.StopOrder,
                Status = "Confirmed"
            });

            booking.Legs.Add(leg);
            booking.TotalAmount += fare;
        }

        _db.Bookings.Add(booking);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return MapBooking(booking);
    }

    public async Task<List<BookingResponseDto>> GetMyBookingsAsync(
        int userId)
    {
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.Legs)
                .ThenInclude(l => l.FromStop)
            .Include(b => b.Legs)
                .ThenInclude(l => l.ToStop)
            .Include(b => b.Legs)
                .ThenInclude(l => l.Trip)
                    .ThenInclude(t => t.Bus)
            .Include(b => b.Legs)
                .ThenInclude(l => l.SeatReservations)
                    .ThenInclude(sr => sr.BusSeat)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync();

        return bookings.Select(MapBooking).ToList();
    }

    public async Task<bool> CancelAsync(int userId, int bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Legs)
                .ThenInclude(l => l.SeatReservations)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId && b.UserId == userId);

        if (booking == null)
            return false;

        if (booking.Status != "Confirmed")
            throw new ArgumentException(
                "Only confirmed bookings can be cancelled.");

        booking.Status = "Cancelled";

        foreach (var leg in booking.Legs)
        {
            foreach (var reservation in leg.SeatReservations)
            {
                reservation.Status = "Cancelled";
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    private static BookingResponseDto MapBooking(Booking booking)
    {
        return new BookingResponseDto
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            Status = booking.Status,
            TotalAmount = booking.TotalAmount,
            CreatedAtUtc = booking.CreatedAtUtc,

            Legs = booking.Legs.Select(leg =>
                new BookingLegResponseDto
                {
                    TripId = leg.TripId,
                    BusNumber = leg.Trip.Bus.BusNumber,
                    FromStop = leg.FromStop.Name,
                    ToStop = leg.ToStop.Name,
                    Fare = leg.Fare,
                    SeatNumber = leg.SeatReservations
                        .FirstOrDefault()?.BusSeat.SeatNumber ?? ""
                }).ToList()
        };
    }
}