using System.Text.Json.Serialization;

public class MachineInputRatioDto
{
    [JsonPropertyName("copper")]
    public int? Copper { get; set; }

    [JsonPropertyName("silicon")]
    public int? Silicon { get; set; }

    [JsonPropertyName("plastic")]
    public int? Plastic { get; set; }

    [JsonPropertyName("gold")]
    public int? Gold { get; set; }

}


public class ThohMachineInfo
{
    [JsonPropertyName("machineName")]
    public required string MachineName { get; set; }

    [JsonPropertyName("inputs")]
    public string? Inputs { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("inputRatio")]
    public required MachineInputRatioDto InputRatio { get; set; }

    [JsonPropertyName("productionRate")]
    public int ProductionRate { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }
}

public class ThohMachineListResponse
{
    [JsonPropertyName("machines")]
    public List<ThohMachineInfo> Machines { get; set; } = new();
}

public class ThohMachinePurchaseRequest
{
    [JsonPropertyName("machineName")]
    public required string MachineName { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public class MachineDetailsDto
{
    [JsonPropertyName("requiredMaterials")]
    public string? RequiredMaterials { get; set; }

    [JsonPropertyName("inputRatio")]
    public Dictionary<string, int> InputRatio { get; set; } = new();

    [JsonPropertyName("productionRate")]
    public int ProductionRate { get; set; }
}

public class ThohMachinePurchaseResponse
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("machineName")]
    public required string MachineName { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; set; }

    [JsonPropertyName("unitWeight")]
    public decimal UnitWeight { get; set; }

    [JsonPropertyName("totalWeight")]
    public decimal TotalWeight { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("machineDetails")]
    public required MachineDetailsDto MachineDetails { get; set; }

    [JsonPropertyName("bankAccount")]
    public string? BankAccount { get; set; }
}