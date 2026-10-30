namespace BusBookingApi.Entities;

public class Booking
{
    public int Id { get; set; }

    public string BookingReference { get; set; } = string.Empty;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Status { get; set; } = "Confirmed";

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<BookingLeg> Legs { get; set; }
        = new List<BookingLeg>();
}