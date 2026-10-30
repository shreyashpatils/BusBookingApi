using BusBookingApi.DTOs;

namespace BusBookingApi.Interfaces;

public interface IBookingService
{
    Task<BookingResponseDto> CreateAsync(
        int userId,
        CreateBookingDto dto);

    Task<List<BookingResponseDto>> GetMyBookingsAsync(
        int userId);

    Task<bool> CancelAsync(int userId, int bookingId);
}