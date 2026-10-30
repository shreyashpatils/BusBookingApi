using BusBookingApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BusBookingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusController : ControllerBase
{
    private readonly IBusService _busService;

    public BusController(IBusService busService)
    {
        _busService = busService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] int fromStopId,
        [FromQuery] int toStopId,
        [FromQuery] DateOnly travelDate)
    {
        if (fromStopId <= 0 || toStopId <= 0 ||
            fromStopId == toStopId)
        {
            return BadRequest(new
            {
                message = "Select two different valid stop IDs."
            });
        }

        var results = await _busService.SearchBusesAsync(
            fromStopId, toStopId, travelDate);

        return Ok(results);
    }
}