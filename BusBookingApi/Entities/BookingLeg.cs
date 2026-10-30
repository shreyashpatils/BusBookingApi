namespace BusBookingApi.Entities;

public class BookingLeg
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public int TripId { get; set; }

    public Trip Trip { get; set; } = null!;

    public int FromStopId { get; set; }

    public Stop FromStop { get; set; } = null!;

    public int ToStopId { get; set; }

    public Stop ToStop { get; set; } = null!;

    public int FromStopOrder { get; set; }

    public int ToStopOrder { get; set; }

    public decimal Fare { get; set; }

    public ICollection<SeatReservation> SeatReservations { get; set; }
        = new List<SeatReservation>();
}