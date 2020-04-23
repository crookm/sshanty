using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Sshanty.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("test")]
        public string Test()
        {
            return "Hello, world";
        }
    }
}
