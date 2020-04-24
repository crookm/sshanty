using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly HttpClient _http;

        public RemoteMediaWorker(ILogger<RemoteMediaWorker> logger, IConfiguration config, MediaFileService mediaFileService, MediaInformationService mediaInformationService)
        {
            _logger = logger;
            _config = config;
            _mediaFileService = mediaFileService;
            _mediaInformationService = mediaInformationService;

            _http = new HttpClient();
            var authBytes = Encoding.ASCII.GetBytes(_config["RemoteHttp:HttpAuth"]);
            var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            _http.DefaultRequestHeaders.Authorization = authHeader;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _logger.LogDebug("Began executing task");
                var sw = Stopwatch.StartNew();
                var filesDownloaded = 0;
                var filesSkipped = 0;

                var connectionInfo = new ConnectionInfo(
                    _config["Remote:HostName"], _config["Remote:UserName"],
                    new PrivateKeyAuthenticationMethod(
                        _config["Remote:UserName"], new PrivateKeyFile(_config["Remote:PrivateKeyFile"])));

                using (var sftp = new SftpClient(connectionInfo))
                {
                    sftp.Connect();
                    var files = ExploreDirectoryForFiles(sftp, _config["Directories:RemoteBase"], token);
                    files.RemoveAll(x => _mediaFileService.ImpliedFileType(x) != FileType.Video);
                    foreach (var file in files)
                    {
                        if (token.IsCancellationRequested)
                            break;
                        var contract = _mediaInformationService.GetMediaInformation(file, token);
                        if (contract.Success)
                        {
                            var localPath = _mediaFileService.GenerateFullLocalPath(contract);
                            if (!localPath.Directory.Exists)
                                localPath.Directory.Create();
                            if (!localPath.Exists)
                            {
                                _logger.LogInformation("Downloading {0} ({1})",
                                    contract.Title.FirstOrDefault(),
                                    contract.Type == MediaType.Episode
                                        ? string.Format("S{0:00}E{1:00}", contract.Season, contract.Episode)
                                        : contract.Type == MediaType.Movie
                                            ? contract.Year.ToString()
                                            : Path.GetFileName(file));
                                _logger.LogDebug("Downloading {0} to {1} ...", Path.GetFileName(file), localPath.FullName);
                                var url = _config["RemoteHttp:HttpBase"] + file.Remove(0, _config["Directories:RemoteBase"].Length);
                                try
                                {
                                    using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                                    response.EnsureSuccessStatusCode();
                                    using (var dataStream = await response.Content.ReadAsStreamAsync())
                                    using (var fileStream = new FileStream(localPath.FullName, FileMode.CreateNew))
                                        await dataStream.CopyToAsync(fileStream, token);
                                    _logger.LogDebug("Finished download");
                                    filesDownloaded++;
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, "Failed to download from {0}", url);
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Skipped downloading {0} ({1}) as it already exists",
                                    contract.Title.FirstOrDefault(),
                                    contract.Type == MediaType.Episode
                                        ? string.Format("S{0:00}E{1:00}", contract.Season, contract.Episode)
                                        : contract.Type == MediaType.Movie
                                            ? contract.Year.ToString()
                                            : Path.GetFileName(file));
                                filesSkipped++;
                            }
                        }
                    }
                }

                sw.Stop();
                _logger.LogInformation("✔️ Completed cycle! Downloaded {0} and skipped {1}", filesDownloaded, filesSkipped);
                _logger.LogDebug("Finished executing task in {0}", sw.Elapsed);
                await Task.Delay(CYCLE_PERIOD, token);
            }
        }

        private List<string> ExploreDirectoryForFiles(SftpClient sftp, string path, CancellationToken token = default, int depth = 0)
        {
            var discoveredFiles = new List<string>();
            if (token.IsCancellationRequested)
                return discoveredFiles;
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
                    discoveredFiles.AddRange(ExploreDirectoryForFiles(sftp, item.FullName, token, depth + 1));
                }
            }

            return discoveredFiles;
        }
    }
}