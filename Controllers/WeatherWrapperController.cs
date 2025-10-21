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
        public async Task<IActionResult> GetLocationAsync(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return BadRequest("Location attribute cannot be empty");
            }
            //Dictionary<string, string> queryParams = validateAndRebuildParams();
            Result<WeatherObject> results = await _apiClient.GetDataAsync(location);
            return Ok(results);
        }


        [HttpGet("{location}/last{numDays}days")]
        public async Task<IActionResult> GetTimeSpanAsync(string location, int numDays)
        {
            if (string.IsNullOrEmpty(location))
            {
                return BadRequest("Location attribute cannot be empty");
            }
            if (numDays <= 0)
            {
                return BadRequest("Number of days cannot be less than 1");
            }
            // Validate params
            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate.AddDays(numDays * -1);
            // Add your logic here using startDate and endDate
            Result<WeatherObject> results = await _apiClient.GetDataAsync(
                location,
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd")
                );
            return Ok(results);
        }


        [HttpGet("{location}/{startDate}")]
        public async Task<IActionResult> GetDateAsync(string location, string startDate)
        {
            if (string.IsNullOrEmpty(location))
            {
                return BadRequest("Location attribute cannot be empty");
            }
            var dateOfInterest = DateTime.Parse(startDate);
            // Check if valid date
            Result<WeatherObject> results = await _apiClient.GetDataAsync(
                location: location,
                startDate: dateOfInterest.ToString("yyyy-MM-dd")
                );
            return Ok(results);
        }


        [HttpGet("{location}/{startDateStr}/{endDateStr}")]
        public async Task<IActionResult> GetTimeRangeAsync(string location, string startDateStr, string endDateStr)
        {
            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate = DateTime.Parse(endDateStr);

            if (string.IsNullOrEmpty(location))
            {
                return BadRequest("Location attribute cannot be empty");
            }
            if (endDate < startDate)
            {
                return BadRequest("Start date cannot be after end date");
            }
            Result<WeatherObject> results = await _apiClient.GetDataAsync(location, startDateStr, endDateStr);
            return Ok(results);
        }
    }
}
