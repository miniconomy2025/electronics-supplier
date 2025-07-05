using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("order_statuses")]
    public class OrderStatus
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }
        
        [Column("status")]
        public string Status { get; set; } = string.Empty;
    }
} 