using System.Text.Json;
using BookingSuite.Controllers.Api;
using BookingSuite.Data;
using BookingSuite.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database: SQLite file (swap connection string for SQL Server later).
builder.Services.AddDbContext<BookingDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=bookingsuite.db"));

builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddSingleton<INotificationService, MailNotificationService>();
builder.Services.AddSingleton<IPaymentService, ManualPaymentService>();

// Real admin login: HttpOnly cookie. API calls get 401 (not a redirect).
builder.Services.AddAuthentication(AdminController.Scheme)
    .AddCookie(AdminController.Scheme, o =>
    {
        o.Cookie.Name = "bs_admin";
        o.Cookie.HttpOnly = true;
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect("/Admin");
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// Seed + auto-create schema.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    DbSeeder.EnsureSeeded(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // /api/* attribute routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
