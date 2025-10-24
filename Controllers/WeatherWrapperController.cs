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
            return Ok(ResultFactory.Success<WeatherObject>(default));
        }

        [HttpGet("{location}")]
        public async Task<IActionResult> GetLocationAsync(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                var resp = ResultFactory.Error<WeatherObject>("Location attribute cannot be empty", HttpContext.Request.Path.ToString(), 400, "Bad API Request");
                return BadRequest(resp);
            }
            //Dictionary<string, string> queryParams = validateAndRebuildParams();
            Result<WeatherObject> results = await _apiClient.GetDataAsync(HttpContext, location);
            return Ok(results);
        }


        [HttpGet("{location}/last{numDays}days")]
        public async Task<IActionResult> GetTimeSpanAsync(string location, int numDays)
        {
            if (string.IsNullOrEmpty(location))
            {
                var resp = ResultFactory.Error<WeatherObject>("Location attribute cannot be empty", HttpContext.Request.Path.ToString(), 400, "Bad API Request");
                return BadRequest(resp);
            }
            if (numDays <= 0)
            {
                var resp = ResultFactory.Error<WeatherObject>("Number of days has to be greater than 0", HttpContext.Request.Path.ToString(), 400, "Bad API Request");
                return BadRequest(resp);
            }
            // Validate params
            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate.AddDays(numDays * -1);
            // Add your logic here using startDate and endDate
            Result<WeatherObject> results = await _apiClient.GetDataAsync(
                httpContext: HttpContext,
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
                var resp = ResultFactory.Error<WeatherObject>("Location attribute cannot be empty", HttpContext.Request.Path.ToString(), 400, "Bad API Request");
                return BadRequest(resp);
            }

            try
            {
                var dateOfInterest = DateTime.Parse(startDate);

                Result<WeatherObject> results = await _apiClient.GetDataAsync(
                     httpContext: HttpContext,
                        location: location,
                        startDate: dateOfInterest.ToString("yyyy-MM-dd")
                        );
                return Ok(results);
            }
            catch (Exception ex)
            {
                var resp = ResultFactory.Error<WeatherObject>("Invalid date passed", HttpContext.Request.Path.ToString(), 400, "Bad API Request");
                return BadRequest(resp);
            }

        }


        [HttpGet("{location}/{startDateStr}/{endDateStr}")]
        public async Task<IActionResult> GetTimeRangeAsync(string location, string startDateStr, string endDateStr)
        {
            try
            {

                DateTime startDate = DateTime.Parse(startDateStr);
                DateTime endDate = DateTime.Parse(endDateStr);

                if (string.IsNullOrEmpty(location))
                {
                    var resp = ResultFactory.Error<WeatherObject>("Location attribute cannot be empty", HttpContext.Request.Path.ToString(), 400, "Bad API Request");
                    return BadRequest(resp);
                }
                if (endDate < startDate)
                {
                    var resp = ResultFactory.Error<WeatherObject>("Start date cannot be after End date", HttpContext.Request.Path.ToString(), 400, "Bad API Request");
                    return BadRequest(resp);
                }
                Result<WeatherObject> results = await _apiClient.GetDataAsync(httpContext: HttpContext, location, startDateStr, endDateStr);
                return Ok(results);
            }
            catch (Exception ex)
            {
                var resp = ResultFactory.Error<WeatherObject>("Invalid date passed", HttpContext.Request.Path.ToString(), 400, "Bad API Request");
                return BadRequest(resp);
            }
        }
    }
}
