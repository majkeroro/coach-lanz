using System.Text.Json;
using BookingSuite.Data;
using BookingSuite.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingSuite.Services;

// C# port of js/booking-engine.js: generateSlots() + priceCalc().
// Reads live data from the DB so every device sees the same availability.
public class AvailabilityService
{
    private readonly BookingDbContext _db;
    public AvailabilityService(BookingDbContext db) => _db = db;

    public static int ToMin(string t)
    {
        var p = t.Split(':');
        return int.Parse(p[0]) * 60 + int.Parse(p[1]);
    }

    public static string ToTime(int min) =>
        $"{min / 60:D2}:{min % 60:D2}";

    public async Task<List<SlotDto>> GenerateSlotsAsync(string dateStr, string serviceId, string staffId, string? excludeRef = null)
    {
        var svc = await _db.Services.FindAsync(serviceId);
        if (svc is null) return new();
        var biz = await _db.BusinessProfile.FindAsync(1) ?? new BusinessProfile();
        if (!DateOnly.TryParse(dateStr, out var date)) return new();

        var hours = await _db.Hours.FindAsync((int)date.DayOfWeek);
        if (hours?.Open is null || hours.Close is null) return new(); // closed
        if (await _db.BlockedDates.AnyAsync(b => b.Date == dateStr)) return new();

        var todayStr = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
        if (string.Compare(dateStr, todayStr, StringComparison.Ordinal) < 0) return new();

        var open = ToMin(hours.Open);
        var close = ToMin(hours.Close);
        int? bs = hours.BreakStart is null ? null : ToMin(hours.BreakStart);
        int? be = hours.BreakEnd is null ? null : ToMin(hours.BreakEnd);

        var existing = await _db.Bookings
            .Where(b => b.Date == dateStr && b.Status != "cancelled"
                && (excludeRef == null || b.Ref != excludeRef))
            .ToListAsync();
        var staff = await _db.Staff.ToListAsync();
        var capable = staff.Where(s => s.Id != "any" && Serves(s, serviceId)).ToList();

        bool Overlaps(string sid, int t, int end) => existing
            .Where(b => b.StaffId == sid || b.StaffId == "any")
            .Any(b =>
            {
                var bt = ToMin(b.Time);
                var bend = bt + b.Duration;
                return t < bend && (end + biz.Buffer) > bt;
            });

        var slots = new List<SlotDto>();
        var interval = biz.SlotInterval <= 0 ? 30 : biz.SlotInterval;
        for (var t = open; t + svc.Duration <= close; t += interval)
        {
            var end = t + svc.Duration;
            if (bs is not null && be is not null && t < be && end > bs) continue;

            bool available;
            string assign = staffId;
            if (!string.IsNullOrEmpty(staffId) && staffId != "any")
            {
                available = !Overlaps(staffId, t, end);
            }
            else
            {
                var free = capable.FirstOrDefault(s => !Overlaps(s.Id, t, end));
                available = free is not null;
                if (free is not null) assign = free.Id;
            }

            if (dateStr == todayStr)
            {
                var nowMin = DateTime.Now.Hour * 60 + DateTime.Now.Minute + 30;
                if (t < nowMin) continue;
            }
            slots.Add(new SlotDto(ToTime(t), available, assign));
        }
        return slots;
    }

    public async Task<PriceDto> PriceAsync(string serviceId, List<string>? addonIds, string? couponCode)
    {
        var svc = await _db.Services.FindAsync(serviceId);
        var basePrice = svc?.Price ?? 0;
        var addons = addonIds is null || addonIds.Count == 0
            ? new List<Addon>()
            : await _db.Addons.Where(a => addonIds.Contains(a.Id)).ToListAsync();
        var addTotal = addons.Sum(a => a.Price);
        var total = basePrice + addTotal;
        decimal discount = 0;
        CouponDto? cp = null;
        var code = (couponCode ?? "").Trim();
        if (!string.IsNullOrEmpty(code))
        {
            var c = await _db.Coupons.FindAsync(code.ToUpperInvariant())
                ?? await _db.Coupons.FirstOrDefaultAsync(x => x.Code.ToLower() == code.ToLower());
            if (c is not null && total > 0 && (c.MinTotal is null || total >= c.MinTotal))
            {
                discount = c.Type == "percent" ? Math.Round(total * c.Value / 100) : Math.Min(c.Value, total);
                total -= discount;
                cp = new CouponDto(c.Code, c.Type, c.Value, c.Label, c.MinTotal);
            }
        }
        return new PriceDto(basePrice, addTotal, discount, total, cp);
    }

    public async Task<bool> IsSlotFreeAsync(string date, string serviceId, string staffId, string time)
    {
        var slots = await GenerateSlotsAsync(date, serviceId, staffId);
        return slots.Any(s => s.Time == time && s.Available);
    }

    public static bool Serves(StaffMember s, string serviceId)
    {
        if (s.ServicesSpec == "all") return true;
        try
        {
            var ids = JsonSerializer.Deserialize<List<string>>(s.ServicesSpec) ?? new();
            return ids.Contains(serviceId);
        }
        catch { return false; }
    }

    public static object StaffServicesOut(StaffMember s)
    {
        if (s.ServicesSpec == "all") return "all";
        try { return JsonSerializer.Deserialize<List<string>>(s.ServicesSpec) ?? new List<string>(); }
        catch { return new List<string>(); }
    }
}
