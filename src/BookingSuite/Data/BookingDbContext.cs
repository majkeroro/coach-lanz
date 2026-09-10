using BookingSuite.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingSuite.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<BusinessProfile> BusinessProfile => Set<BusinessProfile>();
    public DbSet<ServiceItem> Services => Set<ServiceItem>();
    public DbSet<StaffMember> Staff => Set<StaffMember>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Addon> Addons => Set<Addon>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<BusinessHour> Hours => Set<BusinessHour>();
    public DbSet<BlockedDate> BlockedDates => Set<BlockedDate>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<AdminUser> Admins => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<ServiceItem>().HasKey(s => s.Id);
        m.Entity<StaffMember>().HasKey(s => s.Id);
        m.Entity<Location>().HasKey(l => l.Id);
        m.Entity<Addon>().HasKey(a => a.Id);
        m.Entity<Coupon>().HasKey(c => c.Code);
        m.Entity<BusinessHour>().HasKey(h => h.Day);
        m.Entity<BusinessHour>().Property(h => h.Day).ValueGeneratedNever();
        m.Entity<BusinessProfile>().Property(b => b.Id).ValueGeneratedNever();
        m.Entity<BlockedDate>().HasKey(b => b.Date);
        m.Entity<Booking>().HasKey(b => b.Ref);
        m.Entity<AdminUser>().HasKey(a => a.Username);
    }
}
