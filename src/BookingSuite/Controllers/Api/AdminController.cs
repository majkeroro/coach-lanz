using System.Security.Claims;
using System.Text;
using BookingSuite.Data;
using BookingSuite.Models;
using BookingSuite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingSuite.Controllers.Api;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    public const string Scheme = "AdminCookie";
    private readonly BookingDbContext _db;
    private readonly ConfigService _config;
    private readonly PasswordHasher<AdminUser> _hasher = new();

    public AdminController(BookingDbContext db, ConfigService config)
    { _db = db; _config = config; }

    // POST /api/admin/login — replaces the demo admin123 lock. Sets an HttpOnly cookie.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(a =>
            a.Username.ToLower() == req.Username.ToLower());
        if (admin is null ||
            _hasher.VerifyHashedPassword(admin, admin.PasswordHash, req.Password)
                == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Wrong username or password." });

        var claims = new[] { new Claim(ClaimTypes.Name, admin.Username), new Claim(ClaimTypes.Role, "admin") };
        await HttpContext.SignInAsync(Scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme)),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
        return Ok(new { ok = true, mustChangePassword = admin.MustChangePassword });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(Scheme);
        return Ok(new { ok = true });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var admin = await _db.Admins.FindAsync(User.Identity!.Name!);
        return Ok(new { user = User.Identity.Name, mustChangePassword = admin?.MustChangePassword ?? false });
    }

    [Authorize]
    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.New) || req.New.Length < 8)
            return BadRequest(new { error = "New password must be at least 8 characters." });
        var admin = await _db.Admins.FindAsync(User.Identity!.Name!);
        if (admin is null) return Unauthorized();
        if (_hasher.VerifyHashedPassword(admin, admin.PasswordHash, req.Current)
                == PasswordVerificationResult.Failed)
            return BadRequest(new { error = "Current password is wrong." });
        admin.PasswordHash = _hasher.HashPassword(admin, req.New);
        admin.MustChangePassword = false;
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    public record AdminPatchRequest(string? Date, string? Time, string? Status, string? PayStatus);

    // PATCH /api/admin/bookings/{ref} — full control: any status + payment status.
    [Authorize]
    [HttpPatch("bookings/{ref}")]
    public async Task<ActionResult<BookingDto>> PatchBooking(string @ref, [FromBody] AdminPatchRequest req)
    {
        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Ref.ToLower() == @ref.ToLower());
        if (b is null) return NotFound();
        if (req.Date is not null) b.Date = req.Date;
        if (req.Time is not null) b.Time = req.Time;
        if (req.Status is not null) b.Status = req.Status;
        if (req.PayStatus is not null) b.PayStatus = req.PayStatus;
        await _db.SaveChangesAsync();
        return Ok(BookingMapper.ToDto(b));
    }

    [Authorize]
    [HttpGet("bookings")]
    public async Task<ActionResult<List<BookingDto>>> List(
        [FromQuery] string? q, [FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? status)
    {
        var query = _db.Bookings.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(b => b.Status == status);
        if (!string.IsNullOrWhiteSpace(from)) query = query.Where(b => string.Compare(b.Date, from) >= 0);
        if (!string.IsNullOrWhiteSpace(to)) query = query.Where(b => string.Compare(b.Date, to) <= 0);
        var all = await query.OrderByDescending(b => b.Date).ThenByDescending(b => b.Time).ToListAsync();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var ql = q.ToLower();
            all = all.Where(b => (b.Ref + b.Name + b.Phone + b.ServiceName).ToLower().Contains(ql)).ToList();
        }
        return Ok(all.Select(BookingMapper.ToDto).ToList());
    }

    [Authorize]
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
        var all = await _db.Bookings.ToListAsync();
        var upcoming = all.Where(b => string.Compare(b.Date, today) >= 0 && b.Status != "cancelled")
            .OrderBy(b => b.Date + b.Time).Take(8).Select(BookingMapper.ToDto).ToList();
        return Ok(new StatsDto(
            all.Count,
            all.Count(b => b.Date == today && b.Status != "cancelled"),
            all.Where(b => b.Status != "cancelled").Sum(b => b.Total),
            upcoming));
    }

    [Authorize]
    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var all = await _db.Bookings.OrderBy(b => b.Date).ThenBy(b => b.Time).ToListAsync();
        var sb = new StringBuilder("ref,date,time,service,staff,customer,phone,total,pay,payStatus,status\n");
        foreach (var b in all)
            sb.AppendLine($"{b.Ref},{b.Date},{b.Time},\"{b.ServiceName}\",\"{b.StaffName}\",\"{b.Name}\",{b.Phone},{b.Total},{b.Pay},{b.PayStatus},{b.Status}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "bookings.csv");
    }

    [Authorize]
    [HttpGet("config")]
    public async Task<ActionResult<ConfigDto>> GetConfig() => Ok(await _config.GetConfigAsync());

    [Authorize]
    [HttpPut("config")]
    public async Task<ActionResult<ConfigDto>> SaveConfig([FromBody] ConfigDto cfg)
        => Ok(await _config.SaveConfigAsync(cfg));
}
