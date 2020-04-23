using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using Sshanty.Services;
using Sshanty.Contracts.Enums;

namespace Sshanty.Workers
{
    public class RemoteMediaWorker : BackgroundService
    {
        public const int CYCLE_PERIOD = 1000 * 60 * 15; // 15 minutes
        public const int DISCOVERY_RECURSE_MAX_DEPTH = 3;

        private readonly ILogger<RemoteMediaWorker> _logger;
        private readonly IConfiguration _config;
        private readonly MediaFileService _mediaFileService;
        private readonly MediaInformationService _mediaInformationService;

        public RemoteMediaWorker(ILogger<RemoteMediaWorker> logger, IConfiguration config, MediaFileService mediaFileService, MediaInformationService mediaInformationService)
        {
            _logger = logger;
            _config = config;
            _mediaFileService = mediaFileService;
            _mediaInformationService = mediaInformationService;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _logger.LogDebug("Began executing task");
                var sw = Stopwatch.StartNew();

                var connectionInfo = new ConnectionInfo(
                    _config["Remote:HostName"], _config["Remote:UserName"],
                    new PrivateKeyAuthenticationMethod(
                        _config["Remote:UserName"], new PrivateKeyFile(_config["Remote:PrivateKeyFile"])));

                using (var sftp = new SftpClient(connectionInfo))
                {
                    sftp.Connect();

                    var files = ExploreDirectoryForFiles(sftp, _config["Directories:RemoteBase"]);
                    files.RemoveAll(x => _mediaFileService.ImpliedFileType(x) != FileType.Video);

                    foreach (var file in files)
                    {
                        if (token.IsCancellationRequested)
                            break;
                        var fileName = Path.GetFileName(file);
                        var info = _mediaInformationService.GetMediaInformation(file, token);
                        if (info.Success)
                        {
                            switch (info.Type)
                            {
                                case MediaType.Episode:
                                    _logger.LogInformation("Episode: {0} - S{1:00}E{2:00}", info.Title, info.Season, info.Episode);
                                    break;
                                case MediaType.Movie:
                                    _logger.LogInformation("Movie: {0} ({1})", info.Title, info.Year);
                                    break;
                            }
                        }
                    }

                    // foreach (var item in list)
                    // {
                    //     if (stoppingToken.IsCancellationRequested)
                    //         break;

                    //     if (item.IsRegularFile)
                    //     {
                    //         _logger.LogInformation("Saving file {0} ({1}b)...", item.Name, item.Length);
                    //         using var stream = new FileStream(item.Name, FileMode.CreateNew);
                    //         sftp.DownloadFile(item.FullName, stream);
                    //     }
                    // }
                }

                // using (var client = new SshClient(connectionInfo))
                // {
                //     client.Connect();
                //     var cmd = client.RunCommand("ls -la");
                //     _logger.LogInformation(cmd.Result);
                // }

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