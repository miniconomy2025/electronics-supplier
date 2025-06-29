namespace esAPI.Models
{
    public class MachineRatio
    {
        public int RatioId { get; set; }
        public int MaterialId { get; set; }
        public int Ratio { get; set; }
        public int MachineId { get; set; }
        public Material? Material { get; set; }
        public Machine? Machine { get; set; }
    }
} 