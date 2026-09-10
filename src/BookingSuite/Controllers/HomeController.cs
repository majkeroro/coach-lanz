using System.Diagnostics;
using System.Text.Json;
using BookingSuite.Models;
using BookingSuite.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingSuite.Controllers;

public class HomeController : Controller
{
    private readonly ConfigService _config;
    private static readonly JsonSerializerOptions JsonOpts = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public HomeController(ConfigService config) => _config = config;

    private async Task<IActionResult> Page(string view, string active)
    {
        var cfg = await _config.GetConfigAsync();
        ViewData["ServerConfig"] = JsonSerializer.Serialize(cfg, JsonOpts);
        ViewData["Active"] = active;
        return View(view);
    }

    public Task<IActionResult> Index() => Page("Index", "home");
    [Route("/Services")] public Task<IActionResult> Services() => Page("Services", "services");
    [Route("/Booking")] public Task<IActionResult> Booking() => Page("Booking", "booking");
    [Route("/Manage")] public Task<IActionResult> Manage() => Page("Manage", "manage");
    [Route("/Admin")] public Task<IActionResult> Admin() => Page("Admin", "admin");

    [Route("/Home/Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View("~/Views/Shared/Error.cshtml",
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
