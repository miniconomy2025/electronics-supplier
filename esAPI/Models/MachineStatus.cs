using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("machine_statuses")]
    public class MachineStatuses
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }
        [Column("status")]
        [Required]
        public string Status { get; set; }
    }
}