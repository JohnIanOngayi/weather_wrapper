using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using weather_wrapper.Models;
using weather_wrapper.Models.Persistence;
using weather_wrapper.Services;

namespace weather_wrapper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherWrapperController : ControllerBase
    {
        //private ILogger _logger;
        //private RedisClient _redisClient;
        private readonly WeatherApiClient _apiClient;

        public WeatherWrapperController(WeatherApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok("It Works");
        }

        [HttpGet("{location}")]
        public async Task<IActionResult> GetLocation(string location, [FromQuery] )
        {
            if (string.IsNullOrEmpty(location))
            {
                return BadRequest("Location attribute cannot be empty");
            }
            validateAndRebuildParams();
            Result<WeatherObject> results = await _apiClient.GetLocationDataAsync(location);
            return Ok(results);
        }

        pu
    }
}
