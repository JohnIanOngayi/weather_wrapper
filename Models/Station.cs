using System;

namespace weather_wrapper.Models
{
    public record class Station
    {
        public double distance { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public int useCount { get; set; }
        public string? id { get; set; }
        public string? name { get; set; }
        public int quality { get; set; }
        public double contribution { get; set; }
    }
}
