using BookingSuite.Data;
using BookingSuite.Models;
using Microsoft.AspNetCore.Identity;

namespace BookingSuite.Data;

// Seeds the same defaults as js/business-config.js (fitness preset).
public static class DbSeeder
{
    public static void EnsureSeeded(BookingDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.BusinessProfile.Any())
        {
            db.BusinessProfile.Add(new BusinessProfile());
        }

        if (!db.Services.Any())
        {
            db.Services.AddRange(
                new ServiceItem { Id = "fit-assess", Name = "Free Assessment", Category = "Training", Duration = 30, Price = 0, Desc = "Goal review, movement screen + plan.", Icon = "🎯" },
                new ServiceItem { Id = "fit-pt", Name = "1-on-1 Personal Training", Category = "Training", Duration = 60, Price = 800, Desc = "Strength, fat-loss or striking fitness. Includes warm-up + cool-down.", Icon = "💪" },
                new ServiceItem { Id = "fit-strike", Name = "Muay Thai / Boxing Fitness", Category = "Training", Duration = 60, Price = 900, Desc = "Pads, bag rounds, footwork. No hard sparring.", Icon = "🥊" },
                new ServiceItem { Id = "fit-mob", Name = "Mobility & Recovery Session", Category = "Recovery", Duration = 45, Price = 600, Desc = "Hips, spine, shoulders. Desk-worker reset.", Icon = "🧘" },
                new ServiceItem { Id = "fit-online", Name = "Online Coaching Call", Category = "Online", Duration = 45, Price = 700, Desc = "Video coaching + program review.", Icon = "💻" });
        }

        if (!db.Locations.Any())
        {
            db.Locations.AddRange(
                new Location { Id = "main", Name = "Main Branch (Cavite)" },
                new Location { Id = "online", Name = "Online / Video Call" });
        }

        if (!db.Staff.Any())
        {
            db.Staff.AddRange(
                new StaffMember { Id = "any", Name = "First Available", Role = "Any specialist", Avatar = "✨", ServicesSpec = "all" },
                new StaffMember { Id = "lanz", Name = "Coach Lanz", Role = "Head Coach", Avatar = "🥊", ServicesSpec = """["fit-assess","fit-pt","fit-strike","fit-mob","fit-online"]""" },
                new StaffMember { Id = "mia", Name = "Mia R.", Role = "Mobility & Recovery", Avatar = "🧘", ServicesSpec = """["fit-mob","fit-assess","fit-pt"]""" },
                new StaffMember { Id = "jv", Name = "JV D.", Role = "Strength & Conditioning", Avatar = "💪", ServicesSpec = """["fit-pt","fit-assess","fit-online"]""" });
        }

        if (!db.Hours.Any())
        {
            for (var d = 1; d <= 5; d++)
                db.Hours.Add(new BusinessHour { Day = d, Open = "08:00", Close = "19:00", BreakStart = "12:00", BreakEnd = "13:00" });
            db.Hours.Add(new BusinessHour { Day = 6, Open = "09:00", Close = "17:00" });
            db.Hours.Add(new BusinessHour { Day = 0, Open = null, Close = null });
        }

        if (!db.Addons.Any())
        {
            db.Addons.AddRange(
                new Addon { Id = "ad-early", Name = "Early-bird / Priority slot", Price = 150 },
                new Addon { Id = "ad-plan", Name = "Printed program / aftercare kit", Price = 250 },
                new Addon { Id = "ad-video", Name = "Video form review", Price = 200 });
        }

        if (!db.Coupons.Any())
        {
            db.Coupons.AddRange(
                new Coupon { Code = "WELCOME10", Type = "percent", Value = 10, Label = "10% off first booking" },
                new Coupon { Code = "FREEASSESS", Type = "fixed", Value = 500, Label = "₱500 off ₱1500+", MinTotal = 1500 });
        }

        if (!db.Admins.Any())
        {
            var hasher = new PasswordHasher<AdminUser>();
            var admin = new AdminUser { Username = "admin", MustChangePassword = true };
            admin.PasswordHash = hasher.HashPassword(admin, "admin123");
            db.Admins.Add(admin);
        }

        db.SaveChanges();
    }
}
