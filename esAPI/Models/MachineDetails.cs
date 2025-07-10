using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("machine_details")]
    public class MachineDetails
    {
        [Key]
        [Column("detail_id")]
        public int DetailId { get; set; }

        [Column("maximum_output")]
        public int MaximumOutput { get; set; }

        [Column("ratio_id")]
        public int RatioId { get; set; }

        [ForeignKey("RatioId")]
        public MachineRatio? MachineRatio { get; set; }
    }
}
