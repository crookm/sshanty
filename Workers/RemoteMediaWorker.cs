using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;

namespace Sshanty.Workers
{
    public class RemoteMediaWorker : BackgroundService
    {
        public const int CYCLE_PERIOD = 1000 * 60 * 15; // 15 minutes

        private readonly ILogger<RemoteMediaWorker> _logger;
        private readonly IConfiguration _config;

        public RemoteMediaWorker(ILogger<RemoteMediaWorker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Began executing task");
                var sw = Stopwatch.StartNew();

                var connectionInfo = new ConnectionInfo(
                    _config["Remote:HostName"], _config["Remote:UserName"],
                    new PrivateKeyAuthenticationMethod(
                        _config["Remote:UserName"], new PrivateKeyFile(_config["Remote:PrivateKeyFile"])));

                using (var client = new SshClient(connectionInfo))
                {
                    client.Connect();
                    var cmd = client.RunCommand("ls -la");
                    _logger.LogInformation(cmd.Result);
                }

                sw.Stop();
                _logger.LogDebug("Finished executing task in {0}", sw.Elapsed);
                await Task.Delay(CYCLE_PERIOD, stoppingToken);
            }
        }
    }
}