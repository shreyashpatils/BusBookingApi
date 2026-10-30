using BusBookingApi.Data;
using BusBookingApi.DTOs;
using BusBookingApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BusBookingApi.Services;

public class BusService : IBusService
{
    private readonly AppDbContext _db;

    public BusService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<BusSearchResultDto>> SearchBusesAsync(
        int fromStopId,
        int toStopId,
        DateOnly travelDate)
    {
        if (fromStopId == toStopId)
            return new List<BusSearchResultDto>();

        var trips = await _db.Trips
            .Where(t => t.IsActive && t.TravelDate == travelDate)
            .Include(t => t.Bus)
                .ThenInclude(b => b.Seats)
            .Include(t => t.BusRoute)
                .ThenInclude(r => r.RouteStops)
            .ToListAsync();

        var results = new List<BusSearchResultDto>();

        foreach (var trip in trips)
        {
            var stops = trip.BusRoute.RouteStops
                .OrderBy(rs => rs.StopOrder)
                .ToList();

            var from = stops.FirstOrDefault(s => s.StopId == fromStopId);
            var to = stops.FirstOrDefault(s => s.StopId == toStopId);

            if (from == null || to == null ||
                from.StopOrder >= to.StopOrder)
                continue;

            var distance = to.DistanceFromStartKm
                         - from.DistanceFromStartKm;

            if (distance <= 0)
                continue;

            var reservedSeats = await _db.SeatReservations
                .Where(sr =>
                    sr.BookingLeg.TripId == trip.Id &&
                    sr.Status == "Confirmed" &&
                    sr.FromStopOrder < to.StopOrder &&
                    sr.ToStopOrder > from.StopOrder)
                .Select(sr => sr.BusSeatId)
                .Distinct()
                .CountAsync();

            var totalSeats = trip.Bus.Seats.Count(s => s.IsActive);

            results.Add(new BusSearchResultDto
            {
                TripId = trip.Id,
                BusNumber = trip.Bus.BusNumber,
                BusName = trip.Bus.BusName,
                RouteName = trip.BusRoute.Name,
                DepartureTime = trip.DepartureTime.ToString("HH:mm"),
                Fare = distance * trip.BaseFarePerKm,
                AvailableSeats = Math.Max(0, totalSeats - reservedSeats)
            });
        }

        return results;
    }
}