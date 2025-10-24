using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace esAPI.DTOs.Simulation
{
    public class SimulationStartRequestDto
    {
        /// <summary>
        /// UNIX epoch timestamp indicating when the actual simulation started
        /// This will be used to adjust internal time calculations
        /// Optional - if not provided, current time will be used
        /// Can be provided as number or string
        /// </summary>
        [JsonPropertyName("epochStartTime")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        [Range(0, long.MaxValue, ErrorMessage = "Epoch start time must be a positive number")]
        public long? EpochStartTime { get; set; }
    }

    public class FlexibleLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue))
                    return null;

                if (long.TryParse(stringValue, out var result))
                    return result;

                throw new JsonException($"Cannot convert string '{stringValue}' to long. Expected a valid integer.");
            }

            throw new JsonException($"Cannot convert {reader.TokenType} to long. Expected a number or string containing a valid integer.");
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
