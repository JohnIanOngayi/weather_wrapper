using System;
namespace weather_wrapper.Models
{
    public record class WeatherObject
    {
        public int queryCost { get; init; }
        public float latitude { get; init; }
        public float longitude { get; init; }
        public required string resolvedAddress { get; init; }
        public required string address { get; init; }
        public required string timeZone { get; init; }
        public float tzoffset { get; init; }
        public List<Day>? days { get; init; }
        public Dictionary<string, Station>? stations { get; init; }
    }
}
