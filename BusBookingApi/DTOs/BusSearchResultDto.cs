namespace BusBookingApi.DTOs;

public class BusSearchResultDto
{
    public int TripId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string DepartureTime { get; set; } = string.Empty;
    public decimal Fare { get; set; }
    public int AvailableSeats { get; set; }
}