namespace esAPI.DTOs.Electronics
{
    public class ProducedElectronicsResultDto
    {
        public int ElectronicsCreated { get; set; }
        public Dictionary<string, int> MaterialsUsed { get; set; } = [];
    }
} 