
using System.ComponentModel.DataAnnotations;

namespace BusBookingApi.DTOs;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
