using System.Collections.Generic;

namespace esAPI.Dtos.ElectronicsDtos
{
    public class ProducedElectronicsResultDto
    {
        public int ElectronicsCreated { get; set; }
        public Dictionary<string, int> MaterialsUsed { get; set; } = new();
    }
} 