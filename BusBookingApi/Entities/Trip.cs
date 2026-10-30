namespace BusBookingApi.Entities;

public class Trip
{
    public int Id { get; set; }

    public int BusId { get; set; }

    public Bus Bus { get; set; } = null!;

    public int BusRouteId { get; set; }

    public BusRoute BusRoute { get; set; } = null!;

    public DateOnly TravelDate { get; set; }

    public TimeOnly DepartureTime { get; set; }

    public decimal BaseFarePerKm { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BookingLeg> BookingLegs { get; set; }
        = new List<BookingLeg>();
}