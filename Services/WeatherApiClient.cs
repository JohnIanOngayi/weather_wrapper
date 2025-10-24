using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using weather_wrapper.Models;
using weather_wrapper.Models.Persistence;

namespace weather_wrapper.Services
{
    public class WeatherApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        //private rea
        /**
         * URL Main Format: https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/[location]/[date1]/[date2]?key=YOUR_API_KEY
         * No [location]: 400 Error - Bad Request - A location must be provided https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/[date1]/[date2]?key=YOUR_API_KEY
         * [location] is comma separated, can be both `London,UK` or latitude, longitude `38.9697,-77.385`
         * No [date] but last{num_days}days:- 200 - Returns todays and yesterdays so DAY(now()) and DAY(now() - 1)
         * [datetime1] only
         * [datetime2] only
         * [date2] > [date1]: 400 Error - Bad Request - Start date cannot be greater than End date
         * 
         * _httpClient.BaseAddress = new Uri("https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/");
         * 
         * NOTES: 
         * Things to do inside the controller:- Quickly finish up in the business layer if possible
         *  - Validate endPoints EG Check if endDate < startDate
         *  - Transform endPoints EG last{num_days}days into startDate, endDate
         *  - Deconstruct, validate and reconstruct params
         *  - Check and rebuild cache before requesting apiClient 
         *      In cache [Yes] Return, 
         *      [No] make request, update cache then Return
         **/

        public WeatherApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            if (httpClient == null) new HttpClient();
            else _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/");

            _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "My Weather Wrapper");

            _configuration = configuration;
        }

        private string BuildQueryString(Dictionary<string, string> queryParams)
        {
            var parameters = new List<string>();

            foreach (var param in queryParams)
            {
                parameters.Add($"{param.Key}={Uri.EscapeDataString(param.Value)}");
            }

            return string.Join("&", parameters);
        }

        public async Task<Result<WeatherObject>> MakeWeatherRequestAsync(HttpContext httpContext, string path)
        {
            try
            {
                var queryParams = httpContext.Items["SanitizedQueryParams"] as Dictionary<string, string> 
                    ?? new Dictionary<string, string>();

                var pathBuilder = new StringBuilder($@"{_httpClient.BaseAddress}{path}");
                var queryString = BuildQueryString(queryParams);

                var fullUriWithParams = $"{pathBuilder}?{queryString}";

                var response = await _httpClient.GetAsync(fullUriWithParams);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    //return Result<WeatherObject>.Failure(
                    //    $"API request failed: {errorContent}",
                    //    (int)response.StatusCode
                    //);
                    return ResultFactory.Error<WeatherObject>(errorContent, path, (int)response.StatusCode, "API Request Failed");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var weatherObject = JsonSerializer.Deserialize<WeatherObject>(jsonContent);

                return ResultFactory.Success<WeatherObject>(weatherObject, httpContext.Request.Path);
            }
            catch (HttpRequestException ex)
            {
                //return Result<WeatherObject>.Failure($"Network error: {ex.Message}");
                return ResultFactory.Exception<WeatherObject>(ex.Message, path, 500, "Network Error");
            }
            catch (JsonException ex)
            {
                //return Result<WeatherObject>.Failure($"JSON parsing error: {ex.Message}");
                return ResultFactory.Exception<WeatherObject>(ex.Message, path, 500, "JSON Parsing Error");
            }
            catch (Exception ex)
            {
                return ResultFactory.Exception<WeatherObject>(ex.Message, path, 500, "Unexpected Error");
            }
        }

        public async Task<Result<WeatherObject>> GetDataAsync(HttpContext httpContext, string location)
        {
            return await MakeWeatherRequestAsync(httpContext, location);
        }

        public async Task<Result<WeatherObject>> GetDataAsync(HttpContext httpContext, string location, string startDate)
        {
            string endPoint = $"{location}/{startDate}";
            return await MakeWeatherRequestAsync(httpContext, endPoint);
        }

        public async Task<Result<WeatherObject>> GetDataAsync(HttpContext httpContext, string location, string startDate, string endDate)
        {
            string endPoint = $"{location}/{startDate}/{endDate}";
            return await MakeWeatherRequestAsync(httpContext, endPoint);
        }
    }
}
