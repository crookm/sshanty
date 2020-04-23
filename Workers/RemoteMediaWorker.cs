using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sshanty.Workers
{
    public class RemoteMediaWorker : BackgroundService
    {
        public const int CYCLE_PERIOD = 1000 * 60 * 15; // 15 minutes

        private readonly ILogger<RemoteMediaWorker> _logger;

        public RemoteMediaWorker(ILogger<RemoteMediaWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Began executing task");
                var sw = Stopwatch.StartNew();

                sw.Stop();
                _logger.LogDebug("Finished executing task in {0}", sw.Elapsed);
                await Task.Delay(CYCLE_PERIOD, stoppingToken);
            }
        }
    }
}