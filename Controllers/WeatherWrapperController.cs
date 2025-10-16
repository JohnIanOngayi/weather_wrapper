using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using weather_wrapper.Services;

namespace weather_wrapper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherWrapperController : ControllerBase
    {
        //private ILogger _logger;
        //private RedisClient _redisClient;
        //private readonly WeatherApiClient _apiClient;

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok("It Works");
        }
    }
}
