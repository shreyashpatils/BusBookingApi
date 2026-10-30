namespace BusBookingApi.Entities;

public class SeatReservation
{
    public int Id { get; set; }

    public int BookingLegId { get; set; }

    public BookingLeg BookingLeg { get; set; } = null!;

    public int BusSeatId { get; set; }

    public BusSeat BusSeat { get; set; } = null!;

    public int FromStopOrder { get; set; }

    public int ToStopOrder { get; set; }

    public string Status { get; set; } = "Confirmed";
}