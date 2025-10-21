//namespace weather_wrapper.Controllers.MiddleWare
//{
//    public class PreControllerMiddleWare
//    {
//        private readonly HttpContext _httpContext;
//        private readonly RequestDelegate _next;
//        public PreControllerMiddleWare(HttpContext context, RequestDelegate next)
//        {
//            this._next = next;
//            this._httpContext = context;
//        }
//        private static bool ValidateAndRebuildParams()
//        {
//            /**
//             * ACCEPTED PARAMS:
//             *  - key (required): APIKEY
//             *  - unitGroup: us, uk, metric, base
//             *  - lang: ar (Arabic), bg (Bulgiarian), cs (Czech), da (Danish), de (German), el (Greek Modern),
//             *          en (English), es (Spanish) ), fa (Farsi), fi (Finnish), fr (French), he Hebrew), hu, (Hungarian),
//             *          it (Italian), ja (Japanese), ko (Korean), nl (Dutch), pl (Polish), pt (Portuguese), ru (Russian),
//             *          sk (Slovakian), sr (Serbian), sv (Swedish), tr (Turkish), uk (Ukranian), vi (Vietnamese) and zh (Chinese)
//             *  - include: specific json key, val pairs
//             *  - elements: tempmax, tempmin etc
//             *  - contentType: csv etc 
//             *      If anything but json EG csv include cannot be null
//             *  - timezone EG timezone=Z
//             *  - maxDistance
//             *  - maxStations
//             *  - elevationDifference
//             *  - locationNames:- provide alt name for location requested
//             *      EG /api/London, UK?&locationNames=london
//             *  - TODO: degreeDayParams
//             */
//            Dictionary<string, string> queryParams = new Dictionary<string, string>();
//            return queryParams;
//        }
//    }
//}
