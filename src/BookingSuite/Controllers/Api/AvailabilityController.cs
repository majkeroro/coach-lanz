using BookingSuite.Models;
using BookingSuite.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingSuite.Controllers.Api;

[ApiController]
[Route("api/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly AvailabilityService _avail;
    public AvailabilityController(AvailabilityService avail) => _avail = avail;

    // GET /api/availability/slots?date=2026-09-20&serviceId=fit-pt&staffId=any
    [HttpGet("slots")]
    public async Task<ActionResult<List<SlotDto>>> Slots(
        [FromQuery] string date, [FromQuery] string serviceId,
        [FromQuery] string staffId = "any", [FromQuery] string? excludeRef = null)
        => Ok(await _avail.GenerateSlotsAsync(date, serviceId, staffId, excludeRef));

    public record PriceRequest(string ServiceId, List<string>? AddonIds, string? Coupon);

    // POST /api/availability/price
    [HttpPost("price")]
    public async Task<ActionResult<PriceDto>> Price([FromBody] PriceRequest req)
        => Ok(await _avail.PriceAsync(req.ServiceId, req.AddonIds, req.Coupon));
}
