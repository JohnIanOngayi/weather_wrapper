using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using weather_wrapper.Models;
using weather_wrapper.Models.Persistence;

namespace weather_wrapper.Services
{
    public class WeatherApiClient
    {
        private readonly HttpClient _httpClient;
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
         **/

        public WeatherApiClient(HttpClient httpClient)
        {
            if (httpClient == null) new HttpClient();
            else _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/");

            _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "My Weather Wrapper");
        }

        public async Task<Result<WeatherObject>> MakeWeatherRequestAsync(string path, Dictionary<string, string> queryParams)
        {
            try
            {
                var uriWithQuery = QueryHelpers.AddQueryString(path, queryParams);

                var response = await _httpClient.GetAsync(uriWithQuery);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Result<WeatherObject>.Failure(
                        $"API request failed: {errorContent}",
                        (int)response.StatusCode
                    );
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var weatherObject = JsonSerializer.Deserialize<WeatherObject>(jsonContent);

                return Result<WeatherObject>.Success(weatherObject);
            }
            catch (HttpRequestException ex)
            {
                return Result<WeatherObject>.Failure($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return Result<WeatherObject>.Failure($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<WeatherObject>.Failure($"Unexpected error: {ex.Message}");
            }
        }

        public async Task<Result<WeatherObject>> GetLocationDataAsync(string location)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("location", location);
            return await MakeWeatherRequestAsync("location", queryParams);
        }
    }
}
