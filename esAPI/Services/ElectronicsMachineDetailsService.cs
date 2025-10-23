using System.Linq;
using System.Threading.Tasks;
using esAPI.Clients;
using esAPI.Data;
using esAPI.Interfaces;
using esAPI.Logging;
using Microsoft.Extensions.Logging;
using System;

namespace esAPI.Services
{
    public interface IElectronicsMachineDetailsService
    {
        Task<bool> SyncElectronicsMachineDetailsAsync();
    }

    public class ElectronicsMachineDetailsService : IElectronicsMachineDetailsService
    {
        private readonly AppDbContext _context;
        private readonly IThohApiClient _thohApiClient;
        private readonly ILogger<ElectronicsMachineDetailsService> _logger;

        public ElectronicsMachineDetailsService(AppDbContext context, IThohApiClient thohApiClient, ILogger<ElectronicsMachineDetailsService> logger)
        {
            _context = context;
            _thohApiClient = thohApiClient;
            _logger = logger;
        }

        public async Task<bool> SyncElectronicsMachineDetailsAsync()
        {
            try
            {
                _logger.LogInformation("[ElectronicsMachine] Querying THOH for electronics machine details");
                var electronicsMachine = await _thohApiClient.GetElectronicsMachineAsync();
                if (electronicsMachine == null)
                {
                    _logger.LogErrorColored("[ElectronicsMachine] Could not retrieve electronics machine details from THOH API");
                    return false;
                }
                _logger.LogInformation("[ElectronicsMachine] Retrieved electronics machine: ProductionRate={ProductionRate}, Price={Price}, InputRatio={InputRatio}", 
                    electronicsMachine.ProductionRate, 
                    electronicsMachine.Price, 
                    string.Join(", ", electronicsMachine.InputRatio.Select(kv => kv.Key + ":" + kv.Value)));
                // Create or update MachineRatio for each input
                foreach (var input in electronicsMachine.InputRatio)
                {
                    var materialName = input.Key.ToLower();
                    var material = _context.Materials.FirstOrDefault(mat => mat.MaterialName.ToLower() == materialName);
                    if (material == null)
                    {
                        _logger.LogInformation("[ElectronicsMachine] Material '{0}' not found in DB, creating it", input.Key);
                        material = new Models.Material
                        {
                            MaterialName = materialName,
                            PricePerKg = 10.0m // Default price - will be updated by material ordering service
                        };
                        _context.Materials.Add(material);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("[ElectronicsMachine] Created material '{0}' with ID {1}", materialName, material.MaterialId);
                    }
                    var ratio = _context.MachineRatios.FirstOrDefault(r => r.MaterialId == material.MaterialId);
                    if (ratio == null)
                    {
                        ratio = new Models.MachineRatio
                        {
                            MaterialId = material.MaterialId,
                            Ratio = input.Value
                        };
                        _context.MachineRatios.Add(ratio);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("[ElectronicsMachine] Created MachineRatio for material '{MaterialKey}'", input.Key);
                    }
                    else
                    {
                        ratio.Ratio = input.Value;
                        _logger.LogInformation("[ElectronicsMachine] Updated MachineRatio for material '{MaterialKey}'", input.Key);
                    }
                }
                await _context.SaveChangesAsync();
                // Create or update MachineDetails
                var detail = _context.MachineDetails.FirstOrDefault(d => d.MaximumOutput == electronicsMachine.ProductionRate);
                if (detail == null)
                {
                    detail = new Models.MachineDetails
                    {
                        MaximumOutput = electronicsMachine.ProductionRate,
                        RatioId = _context.MachineRatios.First().RatioId // Just link to one ratio for now
                    };
                    _context.MachineDetails.Add(detail);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[ElectronicsMachine] Created MachineDetails with output {MaximumOutput}", detail.MaximumOutput);
                }
                else
                {
                    detail.MaximumOutput = electronicsMachine.ProductionRate;
                    _logger.LogInformation("[ElectronicsMachine] Updated MachineDetails output to {MaximumOutput}", detail.MaximumOutput);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("[ElectronicsMachine] Electronics machine details synced");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored("[ElectronicsMachine] Failed to sync electronics machine details from THOH API. Continuing simulation without update");
                _logger.LogError(ex, "[ElectronicsMachine] Exception details: {Message}", ex.Message);
                return false;
            }
        }
    }
}
