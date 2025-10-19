using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Keyless]
public class DailyMaterialConsumption
{
    [Column("material_id")]
    public int MaterialId { get; set; }
    [Column("material_name")]
    public string MaterialName { get; set; } = string.Empty;
    [Column("total_daily_consumption")]
    public long TotalDailyConsumption { get; set; }
}