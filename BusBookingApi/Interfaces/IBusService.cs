using BusBookingApi.DTOs;

namespace BusBookingApi.Interfaces;

public interface IBusService
{
    Task<List<BusSearchResultDto>> SearchBusesAsync(
        int fromStopId,
        int toStopId,
        DateOnly travelDate);
}