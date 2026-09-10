using BookingSuite.Models;

namespace BookingSuite.Services;

// Payments: manual methods for now (Cash / GCash / Card / Bank Transfer),
// recorded per booking with a PayStatus (Pending | Paid | Refunded).
// Online card/GCash checkout (PayMongo / Xendit) plugs in here later:
// implement IPaymentCheckout, redirect from booking confirmation.
public interface IPaymentService
{
    Task RecordAsync(Booking b);
}

public class ManualPaymentService : IPaymentService
{
    private readonly ILogger<ManualPaymentService> _log;
    public ManualPaymentService(ILogger<ManualPaymentService> log) => _log = log;

    public Task RecordAsync(Booking b)
    {
        // No prepayment required (matches business policy). Just audit the intent.
        _log.LogInformation("Payment intent {Ref}: {Pay} {Total} -> {Status}", b.Ref, b.Pay, b.Total, b.PayStatus);
        return Task.CompletedTask;
    }
}
