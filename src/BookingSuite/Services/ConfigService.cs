using BookingSuite.Data;
using BookingSuite.Models;
using BookingSuite.Services;
using Microsoft.EntityFrameworkCore;

namespace BookingSuite.Services;

// Builds the same config shape the old BS.getConfig() returned,
// now from the database instead of localStorage.
public class ConfigService
{
    private readonly BookingDbContext _db;
    public ConfigService(BookingDbContext db) => _db = db;

    public async Task<ConfigDto> GetConfigAsync()
    {
        var b = await _db.BusinessProfile.FindAsync(1) ?? new BusinessProfile();
        var business = new BusinessDto(b.Name, b.Tagline, b.Phone, b.Messenger, b.Address,
            b.Currency, b.Color, b.Type, b.SlotInterval, b.Buffer, b.AdvanceDays, b.CancelHours);
        var locations = await _db.Locations.OrderBy(l => l.Id).ToListAsync();
        var staff = (await _db.Staff.ToListAsync())
            .Select(s => new StaffDto(s.Id, s.Name, s.Role, s.Avatar, AvailabilityService.StaffServicesOut(s)))
            .ToList();
        var hours = (await _db.Hours.ToListAsync())
            .ToDictionary(h => h.Day.ToString(),
                h => h.Open is null ? null : new HourDto(h.Open, h.Close, h.BreakStart, h.BreakEnd));
        var blocked = (await _db.BlockedDates.OrderBy(x => x.Date).ToListAsync())
            .Select(x => x.Date).ToList();
        var addons = await _db.Addons.OrderBy(a => a.Id).ToListAsync();
        var coupons = (await _db.Coupons.OrderBy(c => c.Code).ToListAsync())
            .Select(c => new CouponDto(c.Code, c.Type, c.Value, c.Label, c.MinTotal)).ToList();
        var services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
        var categories = services.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
        var policies = new PoliciesDto(b.PolicyCancel, b.PolicyNoshow, b.PolicyPayment);
        return new ConfigDto(business, locations, staff, hours, blocked, addons, coupons,
            policies, services, categories);
    }

    // Full-replace save from the Admin dashboard (PUT /api/admin/config).
    public async Task<ConfigDto> SaveConfigAsync(ConfigDto cfg)
    {
        var b = await _db.BusinessProfile.FindAsync(1) ?? new BusinessProfile();
        b.Name = cfg.Business.Name; b.Tagline = cfg.Business.Tagline; b.Phone = cfg.Business.Phone;
        b.Messenger = cfg.Business.Messenger; b.Address = cfg.Business.Address;
        b.Currency = cfg.Business.Currency; b.Color = cfg.Business.Color; b.Type = cfg.Business.Type;
        b.SlotInterval = cfg.Business.SlotInterval; b.Buffer = cfg.Business.Buffer;
        b.AdvanceDays = cfg.Business.AdvanceDays; b.CancelHours = cfg.Business.CancelHours;
        b.PolicyCancel = cfg.Policies.Cancel; b.PolicyNoshow = cfg.Policies.Noshow; b.PolicyPayment = cfg.Policies.Payment;

        _db.Services.RemoveRange(_db.Services);
        foreach (var s in cfg.Services)
            _db.Services.Add(new ServiceItem
            {
                Id = string.IsNullOrWhiteSpace(s.Id) ? "s-" + Guid.NewGuid().ToString("N")[..6] : s.Id,
                Name = s.Name, Category = string.IsNullOrWhiteSpace(s.Category) ? "General" : s.Category,
                Duration = s.Duration, Price = s.Price, Desc = s.Desc ?? "", Icon = s.Icon ?? "📅"
            });

        _db.Staff.RemoveRange(_db.Staff);
        foreach (var s in cfg.Staff)
            _db.Staff.Add(new StaffMember
            {
                Id = s.Id, Name = s.Name, Role = s.Role ?? "", Avatar = s.Avatar ?? "👤",
                ServicesSpec = SpecOut(s.Services)
            });

        _db.Locations.RemoveRange(_db.Locations);
        foreach (var l in cfg.Locations) _db.Locations.Add(new Location { Id = l.Id, Name = l.Name });

        _db.Addons.RemoveRange(_db.Addons);
        foreach (var a in cfg.Addons) _db.Addons.Add(new Addon { Id = a.Id, Name = a.Name, Price = a.Price });

        _db.Coupons.RemoveRange(_db.Coupons);
        foreach (var c in cfg.Coupons)
            _db.Coupons.Add(new Coupon { Code = c.Code.ToUpperInvariant(), Type = c.Type, Value = c.Value, Label = c.Label ?? "", MinTotal = c.MinTotal });

        _db.Hours.RemoveRange(_db.Hours);
        foreach (var kv in cfg.Hours)
            if (int.TryParse(kv.Key, out var day))
                _db.Hours.Add(new BusinessHour
                {
                    Day = day, Open = kv.Value?.Open, Close = kv.Value?.Close,
                    BreakStart = kv.Value?.BreakStart, BreakEnd = kv.Value?.BreakEnd
                });

        _db.BlockedDates.RemoveRange(_db.BlockedDates);
        foreach (var d in cfg.BlockedDates.Distinct())
            _db.BlockedDates.Add(new BlockedDate { Date = d });

        await _db.SaveChangesAsync();
        return await GetConfigAsync();
    }

    // Staff services arrive as "all" (string) or a JSON array. After a
    // JSON round-trip "all" is a JsonElement, not a C# string — handle both.
    private static string SpecOut(object? s)
    {
        if (s is string str) return str == "all" ? "all" : str;
        if (s is System.Text.Json.JsonElement el)
        {
            if (el.ValueKind == System.Text.Json.JsonValueKind.String && el.GetString() == "all")
                return "all";
            return el.GetRawText();
        }
        return System.Text.Json.JsonSerializer.Serialize(s);
    }
}
