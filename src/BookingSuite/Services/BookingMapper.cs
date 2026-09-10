using System.Text.Json;
using BookingSuite.Models;

namespace BookingSuite.Services;

public static class BookingMapper
{
    public static BookingDto ToDto(Booking b) => new(
        b.Ref, b.ServiceId, b.ServiceName, b.Duration, b.Price,
        b.StaffId, b.StaffName, b.LocationId, b.LocationName,
        b.Date, b.Time,
        JsonSerializer.Deserialize<List<string>>(b.AddonIdsJson) ?? new(),
        b.Coupon, b.Total, b.Discount, b.Name, b.Phone, b.Email,
        b.Notes, b.Pay, b.PayStatus, b.Status, b.CreatedAt);

    public static string NewRef() =>
        "BK-" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();

    public static string Digits(string s) =>
        new((s ?? "").Where(char.IsDigit).ToArray());
}
