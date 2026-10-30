namespace BusBookingApi.Entities;

public class RouteStop
{
    public int Id { get; set; }

    public int BusRouteId { get; set; }

    public BusRoute BusRoute { get; set; } = null!;

    public int StopId { get; set; }

    public Stop Stop { get; set; } = null!;

    public int StopOrder { get; set; }

    public decimal DistanceFromStartKm { get; set; }
}