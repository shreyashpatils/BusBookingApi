namespace BusBookingApi.DTOs;

public class BookingResponseDto
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<BookingLegResponseDto> Legs { get; set; } = new();
}

public class BookingLegResponseDto
{
    public int TripId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string FromStop { get; set; } = string.Empty;
    public string ToStop { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Fare { get; set; }
}