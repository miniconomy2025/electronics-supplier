using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("electronics_statuses")]
    public class ElectronicsStatus
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }
        [Column("status")]
        public string Status { get; set; } = string.Empty;
    }
} 