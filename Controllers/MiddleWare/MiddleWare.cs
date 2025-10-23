using Microsoft.AspNetCore.Components.Routing;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using weather_wrapper.Models.Persistence;

namespace weather_wrapper.Controllers.MiddleWare
{
    public class PreControllerMiddleWare
    {
        private readonly HttpContext _httpContext;
        private readonly RequestDelegate _next;
        private readonly List<string> validQueryParams = new()
        {
            // Required parameters
            "key",
    
            // Optional main parameters
            "unitGroup",
            "lang",
            "include",
            "elements",
            "options",
            "contentType",
            "iconSet",
            "timezone",
            "maxDistance",
            "maxStations",
            "elevationDifference",
            "locationNames",
            "forecastBasisDate",
            "forecastBasisDay",
    
            // Degree day parameters
            "degreeDayTempFix",
            "degreeDayStartDate",
            "degreeDayTempMaxThreshold",
            "degreeDayTempBase",
            "degreeDayInverse",
            "degreeDayMethod"
        };
        private struct Unit { };
        private static readonly HashSet<string> ValidIanaTimezones = new(StringComparer.OrdinalIgnoreCase)
        {
            // Americas
            "America/New_York", "America/Chicago", "America/Denver", "America/Los_Angeles",
            "America/Phoenix", "America/Anchorage", "America/Honolulu",
            "America/Toronto", "America/Vancouver", "America/Mexico_City",
            "America/Sao_Paulo", "America/Argentina/Buenos_Aires", "America/Lima",
            "America/Bogota", "America/Caracas", "America/Santiago",
            // Europe
            "Europe/London", "Europe/Paris", "Europe/Berlin", "Europe/Rome",
            "Europe/Madrid", "Europe/Amsterdam", "Europe/Brussels", "Europe/Vienna",
            "Europe/Stockholm", "Europe/Oslo", "Europe/Copenhagen", "Europe/Helsinki",
            "Europe/Warsaw", "Europe/Prague", "Europe/Budapest", "Europe/Athens",
            "Europe/Istanbul", "Europe/Moscow", "Europe/Kyiv", "Europe/Lisbon",
            "Europe/Dublin", "Europe/Zurich",
            // Asia
            "Asia/Tokyo", "Asia/Seoul", "Asia/Shanghai", "Asia/Hong_Kong",
            "Asia/Singapore", "Asia/Bangkok", "Asia/Kolkata", "Asia/Dubai",
            "Asia/Karachi", "Asia/Dhaka", "Asia/Jakarta", "Asia/Manila",
            "Asia/Taipei", "Asia/Kuala_Lumpur", "Asia/Tehran", "Asia/Baghdad",
            "Asia/Jerusalem", "Asia/Riyadh", "Asia/Kuwait", "Asia/Qatar",
            // Pacific
            "Pacific/Auckland", "Pacific/Fiji", "Pacific/Guam", "Pacific/Honolulu",
            "Pacific/Port_Moresby", "Pacific/Noumea", "Pacific/Tahiti",
            // Africa
            "Africa/Cairo", "Africa/Johannesburg", "Africa/Lagos", "Africa/Nairobi",
            "Africa/Casablanca", "Africa/Algiers", "Africa/Tunis", "Africa/Accra",
            // Australia
            "Australia/Sydney", "Australia/Melbourne", "Australia/Brisbane",
            "Australia/Perth", "Australia/Adelaide", "Australia/Darwin",
            // Atlantic
            "Atlantic/Azores", "Atlantic/Cape_Verde", "Atlantic/Reykjavik",
            // Special/UTC
            "UTC", "GMT"
        };
        public PreControllerMiddleWare(HttpContext context, RequestDelegate next)
        {
            _httpContext = context;
            _next = next;
        }
        //private async Task<bool> ValidateAndRebuildParams()
        //{
        /**
         * ACCEPTED PARAMS:
         *  - key (required): APIKEY
         *  - unitGroup: us, uk, metric, base
         *  - lang: ar (Arabic), bg (Bulgiarian), cs (Czech), da (Danish), de (German), el (Greek Modern),
         *          en (English), es (Spanish) ), fa (Farsi), fi (Finnish), fr (French), he Hebrew), hu, (Hungarian),
         *          it (Italian), ja (Japanese), ko (Korean), nl (Dutch), pl (Polish), pt (Portuguese), ru (Russian),
         *          sk (Slovakian), sr (Serbian), sv (Swedish), tr (Turkish), uk (Ukranian), vi (Vietnamese) and zh (Chinese)
         *  - include: specific json key, val pairs
         *  - elements: tempmax, tempmin etc [NO HANDLE]
         *  - contentType: csv etc 
         *      If anything but json EG csv include cannot be null
         *  - timezone EG timezone=Z
         *  - maxDistance
         *  - maxStations
         *  - elevationDifference
         *  - locationNames:- provide alt name for location requested
         *      EG /api/London, UK?&locationNames=london
         *  - TODO: degreeDayParams
         */
        // sanitize queryParams
        //IQueryCollection queryStringParams = _httpContext.Request.Query;
        //if (queryStringParams != null) return false;
        //}

        public async Task InvokeAsync()
        {
            if (!_httpContext.Request.Path.StartsWithSegments("/api/weatherwrapper"))
            {
                await _next(_httpContext);
                return;
            }

            try
            {
                var sanitizedParams = ValidateAndSanitize(_httpContext.Request.Query);
                // Store sanitized params in HttpContext.Items for controller access
                _httpContext.Items["SanitizedQueryParams"] = sanitizedParams;
                _httpContext.Items["QueryValidationPassed"] = true;

                await _next(_httpContext);
            }
            catch (Exception ex)
            {
                // Validation failed - respond immediately and short-circuit
                _httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                _httpContext.Response.ContentType = "application/json";

                //var errorResponse = new
                //{
                //    error = "Validation failed",
                //    message = ex.Message,
                //    timestamp = DateTime.UtcNow
                //};

                var errorResp = Result<Unit>.Failure(ex.Message);

                await _httpContext.Response.WriteAsJsonAsync(errorResp);
            }
        }

        private Dictionary<string, string> ValidateAndSanitize(IQueryCollection query)
        {
            List<string> invalidParams = new List<string>();
            foreach (var param in query.Keys.ToArray())
                if (!validQueryParams.Contains(param))
                    invalidParams.Add(param.ToString());

            if (invalidParams.Count > 0)
                throw new ValidationException($"Invalid params passed: {invalidParams.ToArray()}");


            var sanitized = new Dictionary<string, string>();

            // Check required api token
            if (query.TryGetValue("key", out var tokenkey) && !string.IsNullOrEmpty(tokenkey) && !string.IsNullOrWhiteSpace(tokenkey))
                sanitized.Add("key", tokenkey);
            else
                throw new ValidationException("API key is required");


            // Check contentType
            if (query.TryGetValue("contentType", out var contentType))
            {
                string[] validContentTypes = [ "csv", "json" ];
                string sanitizedContentType = contentType.ToString().ToLower();

                if (!validContentTypes.Contains(sanitizedContentType))
                    throw new ValidationException("Invalid contentType. Must be: csv, json");
                else if (sanitizedContentType == "csv")
                {
                    if (!query.TryGetValue("include", out var _))
                        throw new ValidationException("include must be passed with contentType");
                    else if (query.TryGetValue("include", out var contentTypeInclude))
                    {
                        string[] validIncludes = [
                            "days", "hours", "minutes", "alerts", "current", "events"
                        ];
                        string sanitizedContentTypeInclude = contentTypeInclude.ToString().ToLower();
                        string[] sanitizedIndividualContentTypeIncludes = sanitizedContentTypeInclude.Split(",");

                        List<string> invalids = new List<string>();

                        foreach (string item in sanitizedIndividualContentTypeIncludes)
                        {
                            if (!validIncludes.Contains(item.ToLower().Trim()))
                                invalids.Add(item.ToLower().Trim());
                        }

                        if (invalids.Count > 0)
                            throw new ValidationException($"Allowed include options to be passed with contentType=csv are {validIncludes}");
                    }
                }
                sanitized["contentType"] = sanitizedContentType;
            }


            // Check optional unitGroup
            if (query.TryGetValue("units", out var units))
            {
                string[] validUnits = ["metric", "us", "uk"];
                string sanitizedUnits = units.ToString().ToLower();

                if (!validUnits.Contains(sanitizedUnits))
                    throw new ValidationException("Invalid units. Must be: metric, us, or uk");

                sanitized["units"] = sanitizedUnits;
            }


            // Check optional lang
            if (query.TryGetValue("lang", out var lang))
            {
                string[] validLangs = [
                    "ar", "bg", "cs", "da", "de", "el", "en", "es", "fa", "fi", "fr", "he", "hu",
                    "it", "ja", "ko", "nl", "pl", "pt", "ru", "sk", "sr", "sv", "tr", "uk", "vi", "zh"
                ];

                string sanitizedLang = lang.ToString().ToLower();

                if (!validLangs.Contains(sanitizedLang))
                    throw new ValidationException("Invalid lang");

                sanitized["lang"] = sanitizedLang;
            }


            /**
             * Check optional include- multiple and comma separated
             * NOTE: handle this in controller when fetched from cache
             */
            if (query.TryGetValue("include", out var include))
            {
                string[] validIncludes = [
                    "days", "hours", "minutes", "alerts", "current", "events",
                    "obs", "remote", "fcst", "stats", "statsfcst"
                ];

                string sanitizedInclude = include.ToString().ToLower();
                string[] sanitizedIncludes = sanitizedInclude.Split(",");

                List<string> invalids = new List<string>();

                foreach (string item in sanitizedIncludes)
                {
                    if (!validIncludes.Contains(item.ToLower().Trim()))
                        invalids.Add(item.ToLower().Trim());
                }

                if (invalids.Count > 0)
                    throw new ValidationException($"Invalid include: {invalids.ToArray()}");

                sanitized["include"] = sanitizedInclude;
            }


            // Check optional timeZone
            if (query.TryGetValue("timezone", out var timezone))
            {
                string sanitizedTimezone = timezone.ToString();
                if (!ValidIanaTimezones.Contains(sanitizedTimezone))
                    throw new ValidationException($"Invalid timezone: {sanitizedTimezone}");

                sanitized["timezone"] = sanitizedTimezone;
            }

            return sanitized;
        }
    }
}
