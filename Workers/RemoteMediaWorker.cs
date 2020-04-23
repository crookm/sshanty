using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;

namespace Sshanty.Workers
{
    public class RemoteMediaWorker : BackgroundService
    {
        public const int CYCLE_PERIOD = 1000 * 60 * 15; // 15 minutes
        public const int DISCOVERY_RECURSE_MAX_DEPTH = 3;

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
                await Task.Delay(CYCLE_PERIOD, token);
            }
        }

        private List<string> ExploreDirectoryForFiles(SftpClient sftp, string path, int depth = 0)
        {
            var discoveredFiles = new List<string>();
            if (depth >= DISCOVERY_RECURSE_MAX_DEPTH)
            {
                _logger.LogWarning("Reached maximum recursion depth of {0}", DISCOVERY_RECURSE_MAX_DEPTH);
                return discoveredFiles;
            }

            var remoteDirectory = sftp.ListDirectory(path);
            foreach (var item in remoteDirectory)
            {
                if (item.IsRegularFile) discoveredFiles.Add(item.FullName);
                else if (item.IsDirectory && item.Name != ".." && item.Name != ".")
                {
                    _logger.LogDebug("Recursing into directory {0}", item.FullName);
                    discoveredFiles.AddRange(ExploreDirectoryForFiles(sftp, item.FullName, depth+1));
                }
            }

            return discoveredFiles;
        }
    }
}