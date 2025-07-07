namespace esAPI.Configuration;

public class MonitoredMaterialConfig
{
    public required string Name { get; set; }
    public int LowStockThreshold { get; set; }
    public int ReorderAmount { get; set; }
}

public class InventoryConfig
{
    public const string SectionName = "Inventory";

    public List<MonitoredMaterialConfig> MonitoredMaterials { get; set; } = new();
}