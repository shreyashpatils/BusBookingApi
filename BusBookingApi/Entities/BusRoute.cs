namespace BusBookingApi.Entities;

public class BusRoute
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<RouteStop> RouteStops { get; set; }
        = new List<RouteStop>();

    public ICollection<Trip> Trips { get; set; }
        = new List<Trip>();
}