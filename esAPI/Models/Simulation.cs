using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("simulation")]
    public class Simulation
    {
        [Key]
        [Column("simulation_id")]
        public int SimulationId { get; set; }
        
        [Column("day_number")]
        public int DayNumber { get; set; }
    }
}