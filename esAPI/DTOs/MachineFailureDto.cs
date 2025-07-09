using System.ComponentModel.DataAnnotations;

namespace esAPI.DTOs
{
    public class MachineFailureDto
    {
        [Required]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Failure quantity must be at least 1")]
        public int FailureQuantity { get; set; }

        public string? SimulationDate { get; set; }

        public string? SimulationTime { get; set; }
    }
}