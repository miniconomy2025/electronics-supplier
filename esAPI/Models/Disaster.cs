using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("disasters")]
    public class Disaster
    {
        [Key]
        [Column("disaster_id")]
        public int DisasterId { get; set; }

        [Column("broken_at", TypeName = "numeric(1000,3)")]
        public decimal BrokenAt { get; set; }

        [Column("machines_affected")]
        public int MachinesAffected { get; set; }
    }
} 