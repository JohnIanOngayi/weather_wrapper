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

        public WeatherApiClient(HttpClient httpClient)
        {
            if (httpClient == null) new HttpClient();
            else _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/");

            _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "My Weather Wrapper");
        }

        public async Task<Result<WeatherObject>> MakeWeatherRequestAsync(string path)
        {
            try
            {
                //var uriWithQuery = QueryHelpers.AddQueryString(path);

                //var response = await _httpClient.GetAsync(uriWithQuery);
                var response = await _httpClient.GetAsync(path);

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

        public async Task<Result<WeatherObject>> GetDataAsync(string location)
        {
            return await MakeWeatherRequestAsync(location);
        }

        public async Task<Result<WeatherObject>> GetDataAsync(string location, string startDate)
        {
            string endPoint = $"{location}/{startDate}";
            return await MakeWeatherRequestAsync(endPoint);
        }

        public async Task<Result<WeatherObject>> GetDataAsync(string location, string startDate, string endDate)
        {
            string endPoint = $"{location}/{startDate}/{endDate}";
            return await MakeWeatherRequestAsync(endPoint);
        }
    }
}
