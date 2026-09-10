namespace BookingSuite.Models;

// Mirrors js/business-config.js + js/booking-engine.js shapes.
// JSON to the browser uses camelCase (configured in Program.cs).

public class BusinessProfile
{
    public int Id { get; set; } = 1;
    public string Name { get; set; } = "Coach Lanz";
    public string Tagline { get; set; } = "Fitness & Recovery Coaching";
    public string Phone { get; set; } = "0917-000-0000";
    public string Messenger { get; set; } = "https://www.facebook.com/profile.php?id=61590590284675";
    public string Address { get; set; } = "Cavite, Philippines • Online worldwide";
    public string Currency { get; set; } = "₱";
    public string Color { get; set; } = "#c8f04a";
    public string Type { get; set; } = "fitness";
    public int SlotInterval { get; set; } = 30;
    public int Buffer { get; set; } = 10;
    public int AdvanceDays { get; set; } = 30;
    public int CancelHours { get; set; } = 12;
    public string PolicyCancel { get; set; } = "Free cancellation up to 12h before. Inside 12h, please message us to reschedule.";
    public string PolicyNoshow { get; set; } = "No-show = forfeited slot. Message us to rebook.";
    public string PolicyPayment { get; set; } = "Pay via Cash, GCash, or Card on arrival / online. No prepayment required unless stated.";
}

public class ServiceItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "General";
    public int Duration { get; set; } = 30; // minutes
    public decimal Price { get; set; }
    public string Desc { get; set; } = "";
    public string Icon { get; set; } = "📅";
}

public class StaffMember
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Avatar { get; set; } = "👤";
    // "all" or JSON array of service ids, e.g. ["fit-pt","fit-mob"]
    public string ServicesSpec { get; set; } = "all";
}

public class Location
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public class Addon
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class Coupon
{
    public string Code { get; set; } = "";
    public string Type { get; set; } = "percent"; // percent | fixed
    public decimal Value { get; set; }
    public string Label { get; set; } = "";
    public decimal? MinTotal { get; set; }
}

public class BusinessHour
{
    public int Day { get; set; } // 0=Sun .. 6=Sat
    public string? Open { get; set; } // "HH:mm", null = closed
    public string? Close { get; set; }
    public string? BreakStart { get; set; }
    public string? BreakEnd { get; set; }
}

public class BlockedDate
{
    public string Date { get; set; } = ""; // yyyy-MM-dd
}

public class Booking
{
    public string Ref { get; set; } = "";
    public string ServiceId { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public int Duration { get; set; } = 60;
    public decimal Price { get; set; }
    public string StaffId { get; set; } = "any";
    public string StaffName { get; set; } = "";
    public string LocationId { get; set; } = "";
    public string LocationName { get; set; } = "";
    public string Date { get; set; } = ""; // yyyy-MM-dd
    public string Time { get; set; } = ""; // HH:mm
    public string AddonIdsJson { get; set; } = "[]";
    public string Coupon { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Discount { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Pay { get; set; } = "Cash"; // Cash | GCash | Card | Bank Transfer
    public string PayStatus { get; set; } = "Pending"; // Pending | Paid | Refunded
    public string Status { get; set; } = "confirmed"; // pending | confirmed | completed | cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AdminUser
{
    public string Username { get; set; } = "admin";
    public string PasswordHash { get; set; } = "";
    public bool MustChangePassword { get; set; } = true;
}
