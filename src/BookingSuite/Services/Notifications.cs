using System.Net;
using System.Net.Mail;
using BookingSuite.Models;

namespace BookingSuite.Services;

// Notifications: logs everything; also sends real email when Smtp is
// configured in appsettings (Smtp:Host...). SMS (Semaphore/Twilio) can
// plug into this same interface later.
public interface INotificationService
{
    Task BookingCreatedAsync(Booking b);
    Task BookingChangedAsync(Booking b, string change);
}

public class MailNotificationService : INotificationService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<MailNotificationService> _log;
    public MailNotificationService(IConfiguration cfg, ILogger<MailNotificationService> log)
    { _cfg = cfg; _log = log; }

    public Task BookingCreatedAsync(Booking b)
    {
        _log.LogInformation("Booking {Ref}: {Service} {Date} {Time} for {Name} ({Phone}) total {Total}",
            b.Ref, b.ServiceName, b.Date, b.Time, b.Name, b.Phone, b.Total);
        return SendAsync(
            to: b.Email,
            subject: $"Booked ✓ {b.ServiceName} — {b.Date} @ {b.Time} [{b.Ref}]",
            body: $"Hi {b.Name},\n\nYour booking is confirmed:\n{b.ServiceName} • {b.Date} @ {b.Time}\nRef: {b.Ref}\nTotal: {b.Total}\n\nReply to this email or message us to reschedule.\n");
    }

    public Task BookingChangedAsync(Booking b, string change)
    {
        _log.LogInformation("Booking {Ref} {Change}", b.Ref, change);
        return SendAsync(b.Email, $"Booking {b.Ref} — {change}",
            $"Hi {b.Name},\n\nYour booking {b.Ref} was updated: {change}\n{b.ServiceName} • {b.Date} @ {b.Time}\n");
    }

    private async Task SendAsync(string? to, string subject, string body)
    {
        var host = _cfg["Smtp:Host"];
        var admin = _cfg["Smtp:AdminEmail"];
        // Always notify the business owner if configured, even without customer email.
        var targets = new List<string>();
        if (!string.IsNullOrWhiteSpace(to) && to.Contains('@')) targets.Add(to);
        if (!string.IsNullOrWhiteSpace(admin)) targets.Add(admin);
        if (string.IsNullOrWhiteSpace(host) || targets.Count == 0) return;
        try
        {
            using var msg = new MailMessage();
            msg.From = new MailAddress(_cfg["Smtp:From"] ?? admin!);
            foreach (var t in targets.Distinct()) msg.To.Add(t);
            msg.Subject = subject; msg.Body = body;
            using var smtp = new SmtpClient(host, int.Parse(_cfg["Smtp:Port"] ?? "587"))
            { EnableSsl = true, Credentials = new NetworkCredential(_cfg["Smtp:User"], _cfg["Smtp:Pass"]) };
            await smtp.SendMailAsync(msg);
        }
        catch (Exception ex) { _log.LogWarning(ex, "Email send failed"); }
    }
}
