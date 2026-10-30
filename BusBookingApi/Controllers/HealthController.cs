
using BusBookingApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBookingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            await _db.Database.CanConnectAsync();

            return Ok(new
            {
                status = "Healthy",
                database = "Connected",
                timestampUtc = DateTime.UtcNow
            });
        }
        catch (Exception)
        {
            return StatusCode(503, new
            {
                status = "Unhealthy",
                database = "Unavailable",
                timestampUtc = DateTime.UtcNow
            });
        }
    }
}
