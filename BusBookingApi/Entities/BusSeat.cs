namespace BusBookingApi.Entities;

public class BusSeat
{
    public int Id { get; set; }

    public int BusId { get; set; }

    public Bus Bus { get; set; } = null!;

    public string SeatNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}