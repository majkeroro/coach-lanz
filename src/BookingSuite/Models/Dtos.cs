namespace BookingSuite.Models;

// DTOs shaped exactly like the old window.BS.getConfig() output
// so the existing pages keep working via api-client.js.

public record HourDto(string? Open, string? Close, string? BreakStart, string? BreakEnd);
public record StaffDto(string Id, string Name, string Role, string Avatar, object Services);
public record CouponDto(string Code, string Type, decimal Value, string Label, decimal? MinTotal);
public record PoliciesDto(string Cancel, string Noshow, string Payment);
public record BusinessDto(
    string Name, string Tagline, string Phone, string Messenger, string Address,
    string Currency, string Color, string Type, int SlotInterval, int Buffer,
    int AdvanceDays, int CancelHours);
public record ConfigDto(
    BusinessDto Business,
    List<Location> Locations,
    List<StaffDto> Staff,
    Dictionary<string, HourDto?> Hours,
    List<string> BlockedDates,
    List<Addon> Addons,
    List<CouponDto> Coupons,
    PoliciesDto Policies,
    List<ServiceItem> Services,
    List<string> Categories);

public record SlotDto(string Time, bool Available, string AssignStaff);
public record PriceDto(decimal Base, decimal AddTotal, decimal Discount, decimal Total, CouponDto? Coupon);
public record BookingDto(
    string Ref, string ServiceId, string ServiceName, int Duration, decimal Price,
    string StaffId, string StaffName, string LocationId, string LocationName,
    string Date, string Time, List<string> Addons, string Coupon,
    decimal Total, decimal Discount, string Name, string Phone, string Email,
    string Notes, string Pay, string PayStatus, string Status, DateTime CreatedAt);

public record CreateBookingRequest(
    string ServiceId, string StaffId, string LocationId,
    string Date, string Time, List<string>? Addons, string? Coupon,
    string Name, string Phone, string? Email, string? Notes, string? Pay);
public record PatchBookingRequest(string? Date, string? Time, string? Status, string? Phone);
public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string Current, string New);
public record StatsDto(int Total, int Today, decimal Revenue, List<BookingDto> Upcoming);
