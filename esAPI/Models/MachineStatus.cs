using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("machine_statuses")]
    public class MachineStatus
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("status")]
        [Required]
        public required string Status { get; set; }
    }
}