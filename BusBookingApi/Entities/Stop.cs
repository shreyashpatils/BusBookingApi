namespace BusBookingApi.Entities;

public class Stop
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<RouteStop> RouteStops { get; set; }
        = new List<RouteStop>();
}