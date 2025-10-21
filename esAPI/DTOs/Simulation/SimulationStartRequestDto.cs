using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace esAPI.DTOs.Simulation
{
    public class SimulationStartRequestDto
    {
        /// <summary>
        /// UNIX epoch timestamp indicating when the actual simulation started
        /// This will be used to adjust internal time calculations
        /// Optional - if not provided, current time will be used
        /// </summary>
        [JsonPropertyName("epochStartTime")]
        [Range(0, long.MaxValue, ErrorMessage = "Epoch start time must be a positive number")]
        public long? EpochStartTime { get; set; }
    }
}
