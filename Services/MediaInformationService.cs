using System.Threading;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Sshanty.Contracts;

namespace Sshanty.Services
{
    public class MediaInformationService
    {
        private readonly ILogger<MediaInformationService> _logger;

        public MediaInformationService(ILogger<MediaInformationService> logger)
        {
            _logger = logger;
        }

        public MediaInformationContract GetMediaInformation(string fileName, CancellationToken token = default)
        {
            var sw = Stopwatch.StartNew();
            using (var proc = new Process())
            {
                proc.StartInfo = new ProcessStartInfo("guessit")
                {
                    UseShellExecute = false,
                    Arguments = $"-j \"{fileName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                if (!token.IsCancellationRequested)
                {
                    proc.Start();

                    var cancelRegistration = token.Register(() =>
                    {
                        proc.Kill();
                        proc.WaitForExit();
                    });

                    proc.WaitForExit();
                    cancelRegistration.Dispose();

                    var stdErr = proc.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(stdErr))
                        _logger.LogWarning("Error while executing: {0}", stdErr);
                    var stdOut = proc.StandardOutput.ReadToEnd().Trim();
                    if (!string.IsNullOrEmpty(stdOut))
                    {
                        _logger.LogDebug("Completed media information divination: {0}", stdOut);
                        var jsonOptions = new JsonSerializerOptions();
                        jsonOptions.PropertyNameCaseInsensitive = true;
                        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                        var contract = JsonSerializer.Deserialize<MediaInformationContract>(stdOut, jsonOptions);
                        contract.Success = true;
                        return contract;
                    }
                }
            }

            return new MediaInformationContract { Success = false };
        }
    }
}