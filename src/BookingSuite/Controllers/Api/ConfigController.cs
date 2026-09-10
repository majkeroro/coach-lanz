using BookingSuite.Models;
using BookingSuite.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingSuite.Controllers.Api;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly ConfigService _config;
    public ConfigController(ConfigService config) => _config = config;

    [HttpGet]
    public async Task<ActionResult<ConfigDto>> Get() => Ok(await _config.GetConfigAsync());
}
