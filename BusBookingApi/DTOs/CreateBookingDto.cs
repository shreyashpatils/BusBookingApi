using System.ComponentModel.DataAnnotations;

namespace BusBookingApi.DTOs;

public class CreateBookingDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(2)]
    public List<CreateBookingLegDto> Legs { get; set; } = new();
}

public class CreateBookingLegDto
{
    [Range(1, int.MaxValue)]
    public int TripId { get; set; }

    [Range(1, int.MaxValue)]
    public int FromStopId { get; set; }

    [Range(1, int.MaxValue)]
    public int ToStopId { get; set; }

    [Range(1, int.MaxValue)]
    public int BusSeatId { get; set; }
}