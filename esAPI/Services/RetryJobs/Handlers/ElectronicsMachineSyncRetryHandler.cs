using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace esAPI.Services
{
    public class ElectronicsMachineSyncRetryHandler : IRetryHandler<ElectronicsMachineSyncRetryJob>
    {
        private readonly ElectronicsMachineDetailsService _machineDetailsService;
        private readonly ILogger<ElectronicsMachineSyncRetryHandler> _logger;

        public ElectronicsMachineSyncRetryHandler(
            ElectronicsMachineDetailsService machineDetailsService,
            ILogger<ElectronicsMachineSyncRetryHandler> logger)
        {
            _machineDetailsService = machineDetailsService;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(ElectronicsMachineSyncRetryJob job, CancellationToken token)
        {
            _logger.LogInformation("🔁 Retrying THOH electronics machine sync (attempt {RetryAttempt})", job.RetryAttempt);

            try
            {
                bool success = await _machineDetailsService.SyncElectronicsMachineDetailsAsync();
                if (!success)
                {
                    _logger.LogWarning("❌ Retry attempt {RetryAttempt} failed.", job.RetryAttempt);
                    return false; // ✅ Could be re-queued depending on retry strategy
                }

                _logger.LogInformation("✅ Retry attempt {RetryAttempt} succeeded.", job.RetryAttempt);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception during retry attempt {RetryAttempt}", job.RetryAttempt);
                return false;
            }
        }
    }
}
