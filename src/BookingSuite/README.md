# BookingSuite — ASP.NET Core MVC full-stack app

Your static HTML booking suite (`index/services/booking/manage/admin`), now with a real
backend: **ASP.NET Core MVC (.NET 8) + EF Core + SQLite**, cookie-based admin login,
server-side availability, email notifications, and payment-intent recording.

## Run locally

```powershell
cd src/BookingSuite
dotnet run
# open http://localhost:5000 (or the port shown)
```

Pages: `/` (marketing) · `/Services` · `/Booking` · `/Manage` · `/Admin`

First admin login: **admin / admin123** — you'll be prompted to change it.

## How it fits together

- `Views/Home/*.cshtml` — your HTML pages, unchanged in look. Inline scripts now call
  the API (async slots, server-validated booking, phone-verified reschedule/cancel).
- `wwwroot/js/api-client.js` — overrides `window.BS` with server-backed methods.
  Without `window.SERVER_CONFIG` it no-ops and the old localStorage demo still works.
- `Services/AvailabilityService.cs` — C# port of `generateSlots()` + `priceCalc()`.
- `Controllers/Api/` — REST:
  - `GET /api/config` — live catalog (public)
  - `GET /api/availability/slots?date=&serviceId=&staffId=&excludeRef=` (public)
  - `POST /api/availability/price` (public)
  - `POST /api/bookings` (public, 409 on conflict) · `GET /api/bookings/lookup?q=`
  - `PATCH /api/bookings/{ref}` (phone-verified) · `GET /api/bookings/{ref}/ics`
  - `/api/admin/*` — login/logout/me/password, bookings list, stats, export.csv,
    config GET+PUT, booking PATCH (any status + pay status) — cookie auth required.

## Config

- `appsettings.json` → `ConnectionStrings:Default` (SQLite file; point it at SQL Server
  + swap the EF provider to upgrade), `Smtp:*` (optional — booking emails; everything
  is also logged without it).

## Deploy (Azure App Service, free F1 works)

1. `az webapp up --name <your-app> --runtime "DOTNETCORE:8.0" --sku F1`
   or use the included `Dockerfile` with App Service (single container).
2. SQLite note: the `.db` file lives on local disk — fine on a single instance.
   For scale-out/safety, mount persistent storage or move to Azure SQL
   (change connection string + `UseSqlServer`, `dotnet ef migrations add …`).
3. Set `Smtp:*` app settings for email confirmations.

## Roadmap hooks (stubbed, not faked)

- `INotificationService` — logging + SMTP today; SMS (Semaphore/Twilio) plugs in here.
- `IPaymentService` — records Cash/GCash/Card/Bank-Transfer intent + `PayStatus`
  (Pending/Paid/Refunded, editable in Admin). Online checkout (PayMongo/Xendit)
  plugs in as an `IPaymentCheckout` redirect from booking confirmation.
