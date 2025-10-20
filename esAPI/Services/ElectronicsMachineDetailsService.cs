using System.Linq;
using System.Threading.Tasks;
using esAPI.Clients;
using esAPI.Data;
using esAPI.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace esAPI.Services
{
    public class ElectronicsMachineDetailsService
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
                _logger.LogInformation("🔍 Querying THOH for electronics machine details...");
                var electronicsMachine = await _thohApiClient.GetElectronicsMachineAsync();
                if (electronicsMachine == null)
                {
                    _logger.LogError("❌ Could not retrieve electronics machine details from THOH API.");
                    return false;
                }
                _logger.LogInformation($"✅ Retrieved electronics machine: ProductionRate={electronicsMachine.ProductionRate}, Price={electronicsMachine.Price}, InputRatio={string.Join(", ", electronicsMachine.InputRatio.Select(kv => kv.Key + ":" + kv.Value))}");
                // Create or update MachineRatio for each input
                foreach (var input in electronicsMachine.InputRatio)
                {
                    var material = _context.Materials.FirstOrDefault(mat => mat.MaterialName.ToLower() == input.Key.ToLower());
                    if (material == null)
                    {
                        _logger.LogWarning($"⚠️ Material '{input.Key}' not found in DB, skipping ratio.");
                        continue;
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
                        _logger.LogInformation($"🆕 Created MachineRatio for material '{input.Key}'");
                    }
                    else
                    {
                        ratio.Ratio = input.Value;
                        _logger.LogInformation($"🔄 Updated MachineRatio for material '{input.Key}'");
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
                    _logger.LogInformation($"🆕 Created MachineDetails with output {detail.MaximumOutput}");
                }
                else
                {
                    detail.MaximumOutput = electronicsMachine.ProductionRate;
                    _logger.LogInformation($"🔄 Updated MachineDetails output to {detail.MaximumOutput}");
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Electronics machine details synced.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to sync electronics machine details from THOH API. Continuing simulation without update.");
                return false;
            }
        }
    }
}
