using Microsoft.AspNetCore.Components.Routing;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using weather_wrapper.Models.Persistence;

namespace weather_wrapper.Controllers.MiddleWare
{
    public class PreControllerMiddleWare
    {
        private readonly HttpContext _httpContext;
        private readonly RequestDelegate _next;
        private readonly List<string> ValidKeys = new List<string>()
            {
                "key", "lang", "include", "elements", "contentType", "timezone",
                "maxDistance", "maxStations", "elevationDifference", "locationNames"
            };
        public struct Unit { };
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
             *  - elements: tempmax, tempmin etc
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

        public async Task InvokeAsync ()
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
            var sanitized = new Dictionary<string, string>();

            // Check if api token is included
            if (query.TryGetValue("key", out var tokenkey) && !string.IsNullOrEmpty(tokenkey) && !string.IsNullOrWhiteSpace(tokenkey))
                sanitized.Add("key", tokenkey);
            else
                throw new ValidationException("API key is required");

            return sanitized;
        }
    }
}
