using ElectricRaspberry.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ElectricRaspberry.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DiscordController : ControllerBase
{
    private readonly ILogger<DiscordController> _logger;
    private readonly DiscordOptions _options;

    public DiscordController(
        ILogger<DiscordController> logger,
        IOptions<DiscordOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    // GET api/discord/status
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            Status = "Active"
        });
    }
}