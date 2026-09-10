using System.Text;
using BookingSuite.Data;
using BookingSuite.Models;
using BookingSuite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingSuite.Controllers.Api;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly BookingDbContext _db;
    private readonly AvailabilityService _avail;
    private readonly INotificationService _notify;
    private readonly IPaymentService _pay;

    public BookingsController(BookingDbContext db, AvailabilityService avail,
        INotificationService notify, IPaymentService pay)
    { _db = db; _avail = avail; _notify = notify; _pay = pay; }

    // POST /api/bookings — public booking creation, server-validated.
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { error = "Name and mobile are required." });

        var svc = await _db.Services.FindAsync(req.ServiceId);
        if (svc is null) return BadRequest(new { error = "Unknown service." });
        var staff = await _db.Staff.FindAsync(req.StaffId);
        if (staff is null) return BadRequest(new { error = "Unknown staff." });
        var loc = await _db.Locations.FindAsync(req.LocationId);
        if (loc is null) return BadRequest(new { error = "Unknown location." });
        if (!AvailabilityService.Serves(staff, svc.Id) && staff.Id != "any")
            return BadRequest(new { error = "Staff does not offer this service." });

        if (!DateOnly.TryParse(req.Date, out var date))
            return BadRequest(new { error = "Invalid date." });
        var today = DateOnly.FromDateTime(DateTime.Now);
        var biz = await _db.BusinessProfile.FindAsync(1) ?? new BusinessProfile();
        if (date < today) return BadRequest(new { error = "Date is in the past." });
        if (date > today.AddDays(biz.AdvanceDays))
            return BadRequest(new { error = $"Bookings open only {biz.AdvanceDays} days ahead." });

        var slots = await _avail.GenerateSlotsAsync(req.Date, svc.Id, staff.Id);
        var slot = slots.FirstOrDefault(s => s.Time == req.Time && s.Available);
        if (slot is null) return Conflict(new { error = "That slot was just taken. Please pick another time." });

        var price = await _avail.PriceAsync(svc.Id, req.Addons, req.Coupon);
        var pay = string.IsNullOrWhiteSpace(req.Pay) ? "Cash" : req.Pay;

        var b = new Booking
        {
            Ref = await UniqueRefAsync(),
            ServiceId = svc.Id, ServiceName = svc.Name, Duration = svc.Duration, Price = svc.Price,
            StaffId = staff.Id == "any" ? slot.AssignStaff : staff.Id,
            StaffName = staff.Id == "any"
                ? (await _db.Staff.FindAsync(slot.AssignStaff))?.Name ?? "First Available"
                : staff.Name,
            LocationId = loc.Id, LocationName = loc.Name,
            Date = req.Date, Time = req.Time,
            AddonIdsJson = System.Text.Json.JsonSerializer.Serialize(req.Addons ?? new List<string>()),
            Coupon = (req.Coupon ?? "").ToUpperInvariant(),
            Total = price.Total, Discount = price.Discount,
            Name = req.Name.Trim(), Phone = req.Phone.Trim(),
            Email = (req.Email ?? "").Trim(), Notes = (req.Notes ?? "").Trim(),
            Pay = pay, PayStatus = "Pending", Status = "confirmed", CreatedAt = DateTime.UtcNow
        };
        _db.Bookings.Add(b);
        await _db.SaveChangesAsync();
        await _pay.RecordAsync(b);
        _ = _notify.BookingCreatedAsync(b); // fire-and-forget
        return CreatedAtAction(nameof(Ics), new { @ref = b.Ref }, BookingMapper.ToDto(b));
    }

    // GET /api/bookings/lookup?q=BK-XXXXX — customer self-service.
    // Requires knowing the exact ref, the phone, or the name: only matches returned.
    [HttpGet("lookup")]
    public async Task<ActionResult<List<BookingDto>>> Lookup([FromQuery] string q)
    {
        q = (q ?? "").Trim();
        if (q.Length < 3) return Ok(new List<BookingDto>());
        var qd = BookingMapper.Digits(q);
        var all = await _db.Bookings.OrderByDescending(b => b.Date).ThenByDescending(b => b.Time).ToListAsync();
        var matches = all.Where(b =>
            b.Ref.Equals(q, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(qd) && BookingMapper.Digits(b.Phone).Contains(qd)) ||
            b.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        return Ok(matches.Select(BookingMapper.ToDto).ToList());
    }

    // PATCH /api/bookings/{ref} — public reschedule/cancel with phone check; staff/admin unrestricted.
    [HttpPatch("{ref}")]
    public async Task<ActionResult<BookingDto>> Patch(string @ref, [FromBody] PatchBookingRequest req)
    {
        var b = await _db.Bookings.FindAsync(@ref.ToUpperInvariant())
            ?? await _db.Bookings.FirstOrDefaultAsync(x => x.Ref.ToLower() == @ref.ToLower());
        if (b is null) return NotFound(new { error = "Booking not found." });

        var isAdmin = User.Identity?.IsAuthenticated == true;
        if (!isAdmin)
        {
            if (string.IsNullOrWhiteSpace(req.Phone) ||
                BookingMapper.Digits(req.Phone) != BookingMapper.Digits(b.Phone))
                return Forbid();
            if (req.Status is not null && req.Status != "cancelled")
                return BadRequest(new { error = "Only cancellation is allowed here." });
        }

        var changed = new List<string>();
        if (req.Date is not null || req.Time is not null)
        {
            var date = req.Date ?? b.Date;
            var time = req.Time ?? b.Time;
            var slots = await _avail.GenerateSlotsAsync(date, b.ServiceId, b.StaffId, b.Ref);
            if (!slots.Any(s => s.Time == time && s.Available))
                return Conflict(new { error = "Slot taken — pick another." });
            b.Date = date; b.Time = time; changed.Add($"rescheduled to {date} @ {time}");
        }
        if (req.Status is not null) { b.Status = req.Status; changed.Add(req.Status); }

        await _db.SaveChangesAsync();
        if (changed.Count > 0) _ = _notify.BookingChangedAsync(b, string.Join(", ", changed));
        return Ok(BookingMapper.ToDto(b));
    }

    [Authorize]
    [HttpDelete("{ref}")]
    public async Task<IActionResult> Delete(string @ref)
    {
        var b = await _db.Bookings.FindAsync(@ref.ToUpperInvariant())
            ?? await _db.Bookings.FirstOrDefaultAsync(x => x.Ref.ToLower() == @ref.ToLower());
        if (b is null) return NotFound();
        _db.Bookings.Remove(b);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/bookings/{ref}/ics — add-to-calendar file.
    [HttpGet("{ref}/ics")]
    public async Task<IActionResult> Ics(string @ref)
    {
        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Ref.ToLower() == @ref.ToLower());
        if (b is null) return NotFound();
        var dt = b.Date.Replace("-", "") + "T" + b.Time.Replace(":", "") + "00";
        var end = b.Date.Replace("-", "") + "T" + AvailabilityService.ToTime(AvailabilityService.ToMin(b.Time) + b.Duration).Replace(":", "") + "00";
        var ics = string.Join("\r\n", "BEGIN:VCALENDAR", "VERSION:2.0", "BEGIN:VEVENT",
            $"UID:{b.Ref}@bookingsuite", $"DTSTAMP:{dt}", $"DTSTART:{dt}", $"DTEND:{end}",
            $"SUMMARY:{b.ServiceName} ({b.Ref})",
            $"DESCRIPTION:Staff: {b.StaffName}\\nLocation: {b.LocationName}\\nRef: {b.Ref}",
            $"LOCATION:{b.LocationName}", "END:VEVENT", "END:VCALENDAR");
        return File(Encoding.UTF8.GetBytes(ics), "text/calendar", $"{b.Ref}.ics");
    }

    private async Task<string> UniqueRefAsync()
    {
        for (var i = 0; i < 10; i++)
        {
            var r = BookingMapper.NewRef();
            if (!await _db.Bookings.AnyAsync(b => b.Ref == r)) return r;
        }
        return "BK-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    }
}
