namespace BusBookingApi.Entities;

public class Bus
{
    public int Id { get; set; }

    public string BusNumber { get; set; } = string.Empty;

    public string BusName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<BusSeat> Seats { get; set; }
        = new List<BusSeat>();

    public ICollection<Trip> Trips { get; set; }
        = new List<Trip>();
}