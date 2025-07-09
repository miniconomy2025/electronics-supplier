using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("lookup_values")]
    public class LookupValue
    {
        [Key]
        [Column("value_id")]
        public int ValueId { get; set; }

        [Column("electronics_price_per_unit")]
        public decimal ElectronicsPricePerUnit { get; set; }

        [Column("changed_at")]
        public decimal ChangedAt { get; set; }
    }
}