using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace weather_wrapper.Controllers.MiddleWare
{
    public class PreControllerMiddleWare
    {
        private readonly RequestDelegate _next;
        //private readonly ILogger<PreControllerMiddleWare> _logger;

        private static readonly HashSet<string> ValidQueryParams = new(StringComparer.OrdinalIgnoreCase)
        {
            "key", "unitGroup", "lang", "include", "elements", "options",
            "contentType", "iconSet", "timezone", "maxDistance", "maxStations",
            "elevationDifference", "locationNames", "forecastBasisDate", "forecastBasisDay",
            "degreeDayTempFix", "degreeDayStartDate", "degreeDayTempMaxThreshold",
            "degreeDayTempBase", "degreeDayInverse", "degreeDayMethod"
        };

        private static readonly HashSet<string> ValidContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "csv", "json"
        };

        private static readonly HashSet<string> ValidUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "metric", "us", "uk", "base"
        };

        private static readonly HashSet<string> ValidLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "ar", "bg", "cs", "da", "de", "el", "en", "es", "fa", "fi", "fr", "he", "hu",
            "it", "ja", "ko", "nl", "pl", "pt", "ru", "sk", "sr", "sv", "tr", "uk", "vi", "zh"
        };

        private static readonly HashSet<string> ValidIncludes = new(StringComparer.OrdinalIgnoreCase)
        {
            "days", "hours", "minutes", "alerts", "current", "events",
            "obs", "remote", "fcst", "stats", "statsfcst"
        };

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

        public PreControllerMiddleWare(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }
        
        //public PreControllerMiddleWare(RequestDelegate next, ILogger<PreControllerMiddleWare> logger)
        //{
        //    _next = next ?? throw new ArgumentNullException(nameof(next));
        //    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        //}

        public async Task InvokeAsync(HttpContext context)
        {
            // Only process weather wrapper requests
            if (!context.Request.Path.StartsWithSegments("/api/weatherwrapper"))
            {
                await _next(context);
                return;
            }

            try
            {
                var sanitizedParams = ValidateAndSanitize(context.Request.Query);

                // Store sanitized params for controller access
                context.Items["SanitizedQueryParams"] = sanitizedParams;
                context.Items["QueryValidationPassed"] = true;

                //_logger.LogDebug("Query validation passed for {Path}", context.Request.Path);
                await _next(context);
            }
            catch (ValidationException ex)
            {
                //_logger.LogWarning(ex, "Validation failed for {Path}: {Message}",
                    //context.Request.Path, ex.Message);

                await WriteErrorResponse(context, ex.Message);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Unexpected error during validation for {Path}",
                    //context.Request.Path);

                await WriteErrorResponse(context, "An unexpected error occurred during validation");
            }
        }

        private static async Task WriteErrorResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                success = false,
                error = "Validation failed",
                message,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.ToString()
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }

        private Dictionary<string, string> ValidateAndSanitize(IQueryCollection query)
        {
            // 1. Validate all params are recognized
            ValidateParamNames(query);

            var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 2. Validate required API key
            ValidateApiKey(query, sanitized);

            // 3. Validate contentType (affects include validation)
            var contentType = ValidateContentType(query, sanitized);

            // 4. Validate include (depends on contentType)
            ValidateInclude(query, sanitized, contentType);

            // 5. Validate optional params
            ValidateUnitGroup(query, sanitized);
            ValidateLanguage(query, sanitized);
            ValidateTimezone(query, sanitized);
            ValidateNumericParams(query, sanitized);
            ValidateStringParams(query, sanitized);

            return sanitized;
        }

        private static void ValidateParamNames(IQueryCollection query)
        {
            var invalidParams = query.Keys
                .Where(key => !ValidQueryParams.Contains(key))
                .ToList();

            if (invalidParams.Any())
            {
                throw new ValidationException(
                    $"Invalid parameters: {string.Join(", ", invalidParams)}");
            }
        }

        private static void ValidateApiKey(IQueryCollection query, Dictionary<string, string> sanitized)
        {
            if (!query.TryGetValue("key", out var key) ||
                string.IsNullOrWhiteSpace(key.ToString()))
            {
                throw new ValidationException("API key is required");
            }

            var keyValue = key.ToString().Trim();

            // Basic API key format validation (adjust regex to match Visual Crossing format)
            if (!Regex.IsMatch(keyValue, @"^[A-Za-z0-9]{20,}$"))
            {
                throw new ValidationException("API key format is invalid");
            }

            sanitized["key"] = keyValue;
        }

        private static string? ValidateContentType(IQueryCollection query, Dictionary<string, string> sanitized)
        {
            if (!query.TryGetValue("contentType", out var contentType))
            {
                return null;
            }

            var sanitizedContentType = contentType.ToString().Trim();

            if (!ValidContentTypes.Contains(sanitizedContentType))
            {
                throw new ValidationException(
                    $"Invalid contentType '{sanitizedContentType}'. Must be: csv or json");
            }

            sanitized["contentType"] = sanitizedContentType.ToLower();
            return sanitized["contentType"];
        }

        private static void ValidateInclude(
            IQueryCollection query,
            Dictionary<string, string> sanitized,
            string? contentType)
        {
            if (!query.TryGetValue("include", out var include))
            {
                // CSV requires include parameter
                if (contentType?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new ValidationException(
                        "The 'include' parameter is required when contentType is 'csv'");
                }
                return;
            }

            var includeValue = include.ToString().Trim();
            var includeItems = includeValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToLower())
                .ToList();

            if (!includeItems.Any())
            {
                throw new ValidationException("The 'include' parameter cannot be empty");
            }

            var invalidIncludes = includeItems
                .Where(item => !ValidIncludes.Contains(item))
                .ToList();

            if (invalidIncludes.Any())
            {
                throw new ValidationException(
                    $"Invalid include values: {string.Join(", ", invalidIncludes)}. " +
                    $"Valid options: {string.Join(", ", ValidIncludes)}");
            }

            // Store as comma-separated lowercase string
            sanitized["include"] = string.Join(",", includeItems.Distinct());
        }

        private static void ValidateUnitGroup(IQueryCollection query, Dictionary<string, string> sanitized)
        {
            if (!query.TryGetValue("unitGroup", out var units))
            {
                return;
            }

            var sanitizedUnits = units.ToString().Trim();

            if (!ValidUnits.Contains(sanitizedUnits))
            {
                throw new ValidationException(
                    $"Invalid unitGroup '{sanitizedUnits}'. Must be: metric, us, uk, or base");
            }

            sanitized["unitGroup"] = sanitizedUnits.ToLower();
        }

        private static void ValidateLanguage(IQueryCollection query, Dictionary<string, string> sanitized)
        {
            if (!query.TryGetValue("lang", out var lang))
            {
                return;
            }

            var sanitizedLang = lang.ToString().Trim();

            if (!ValidLanguages.Contains(sanitizedLang))
            {
                throw new ValidationException(
                    $"Invalid language code '{sanitizedLang}'");
            }

            sanitized["lang"] = sanitizedLang.ToLower();
        }

        private static void ValidateTimezone(IQueryCollection query, Dictionary<string, string> sanitized)
        {
            if (!query.TryGetValue("timezone", out var timezone))
            {
                return;
            }

            var sanitizedTimezone = timezone.ToString().Trim();

            if (!ValidIanaTimezones.Contains(sanitizedTimezone))
            {
                throw new ValidationException(
                    $"Invalid timezone '{sanitizedTimezone}'. Must be a valid IANA timezone " +
                    $"(e.g., 'America/New_York', 'Europe/London')");
            }

            sanitized["timezone"] = sanitizedTimezone;
        }

        private static void ValidateNumericParams(IQueryCollection query, Dictionary<string, string> sanitized)
        {
            // maxDistance validation
            if (query.TryGetValue("maxDistance", out var maxDistance))
            {
                if (!int.TryParse(maxDistance, out var distance) || distance < 0)
                {
                    throw new ValidationException(
                        "maxDistance must be a non-negative integer");
                }
                sanitized["maxDistance"] = distance.ToString();
            }

            // maxStations validation
            if (query.TryGetValue("maxStations", out var maxStations))
            {
                if (!int.TryParse(maxStations, out var stations) || stations < 1)
                {
                    throw new ValidationException(
                        "maxStations must be a positive integer");
                }
                sanitized["maxStations"] = stations.ToString();
            }

            // elevationDifference validation
            if (query.TryGetValue("elevationDifference", out var elevDiff))
            {
                if (!int.TryParse(elevDiff, out var diff) || diff < 0)
                {
                    throw new ValidationException(
                        "elevationDifference must be a non-negative integer");
                }
                sanitized["elevationDifference"] = diff.ToString();
            }
        }

        private static void ValidateStringParams(IQueryCollection query, Dictionary<string, string> sanitized)
        {
            // elements - comma-separated list (no strict validation, let API handle)
            if (query.TryGetValue("elements", out var elements))
            {
                var sanitizedElements = elements.ToString().Trim();
                if (sanitizedElements.Length > 500)
                {
                    throw new ValidationException("elements parameter is too long");
                }
                sanitized["elements"] = sanitizedElements;
            }

            // locationNames validation
            if (query.TryGetValue("locationNames", out var locationNames))
            {
                var sanitizedNames = locationNames.ToString().Trim();

                // Only allow alphanumeric, spaces, commas, hyphens
                if (!Regex.IsMatch(sanitizedNames, @"^[a-zA-Z0-9\s,\-]+$"))
                {
                    throw new ValidationException(
                        "locationNames contains invalid characters");
                }

                if (sanitizedNames.Length > 200)
                {
                    throw new ValidationException("locationNames is too long");
                }

                sanitized["locationNames"] = sanitizedNames;
            }

            // iconSet validation
            if (query.TryGetValue("iconSet", out var iconSet))
            {
                var validIconSets = new[] { "icons1", "icons2" };
                var sanitizedIconSet = iconSet.ToString().Trim().ToLower();

                if (!validIconSets.Contains(sanitizedIconSet))
                {
                    throw new ValidationException(
                        $"Invalid iconSet '{sanitizedIconSet}'. Must be: icons1 or icons2");
                }

                sanitized["iconSet"] = sanitizedIconSet;
            }
        }
    }
}