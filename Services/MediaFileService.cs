using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Renci.SshNet.Sftp;
using Sshanty.Contracts;
using Sshanty.Contracts.Enums;

namespace Sshanty.Services
{
    public class MediaFileService
    {
        public string[] VideoExtensions = new[] { ".mkv", ".mp4", ".avi", ".m4v", ".webm" };
        public string[] AudioExtensions = new[] { ".mp3", ".m4a", ".flac", ".alac", ".aac", ".weba", ".ogg", ".wav" };

        private readonly ILogger<MediaFileService> _logger;
        private readonly IConfiguration _config;

        public MediaFileService(ILogger<MediaFileService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public FileType ImpliedFileType(SftpFile file)
        {
            var extension = Path.GetExtension(file.FullName);

            if (string.IsNullOrEmpty(extension))
                return FileType.Other;
            if (VideoExtensions.Contains(extension))
                return FileType.Video;
            if (AudioExtensions.Contains(extension))
                return FileType.Audio;

            return FileType.Other;
        }

        public FileInfo GenerateFullLocalPath(MediaInformationContract contract)
        {
            var paths = new List<string>();
            paths.Add(_config["Directories:LocalBase"]);

            if (string.IsNullOrEmpty(contract.Container))
                throw new ArgumentNullException("All media types require container");

            var textInfo = new CultureInfo("en-US", false).TextInfo;
            switch (contract.Type)
            {
                case MediaType.Episode:
                    if (contract.Title.Count == 0 ||
                        !contract.Season.HasValue ||
                        !contract.Episode.HasValue)
                        throw new ArgumentNullException("Series must have a title, season, and episode");
                    paths.Add("tv");
                    contract.Title[0] = ConvertToAlphaNumSortableTitle(contract.Title[0]);
                    var seriesTitle = string.Join(" - ", contract.Title);
                    seriesTitle = textInfo.ToTitleCase(seriesTitle);
                    paths.Add(seriesTitle);
                    paths.Add(string.Format("Season {0}", contract.Season));
                    paths.Add(string.Format("S{0:00}E{1:00}.{2}",
                        contract.Season, contract.Episode, contract.Container));
                    break;
                case MediaType.Movie:
                    if (contract.Title.Count == 0 ||
                        !contract.Year.HasValue)
                        throw new ArgumentNullException("Movies must have a title and year");
                    paths.Add("movies");
                    contract.Title[0] = ConvertToAlphaNumSortableTitle(contract.Title[0]);
                    var title = string.Join(" - ", contract.Title);
                    if (!string.IsNullOrEmpty(contract.AlternativeTitle))
                        title += string.Format(" - {0}", contract.AlternativeTitle);
                    title = textInfo.ToTitleCase(title);
                    title += string.Format(" ({0}).{1}", contract.Year, contract.Container);
                    paths.Add(title);
                    break;
                default:
                    throw new NotImplementedException("Media type not implemented");
            }

            return new FileInfo(Path.Combine(paths.ToArray()));
        }

        private string ConvertToAlphaNumSortableTitle(string mainTitle)
        {
            // If the title starts with 'the', move it to the end to enable better sorting in filesystems
            var outTitle = mainTitle;
            var mainTitleWords = mainTitle.Split().ToList();
            if (mainTitleWords.Count > 1 &&
                string.Equals(mainTitleWords.First(), "the", StringComparison.OrdinalIgnoreCase))
            {
                mainTitleWords.RemoveAt(0);
                mainTitleWords[^1] = mainTitleWords[^1] + ",";
                mainTitleWords.Add("The");
                outTitle = string.Join(" ", mainTitleWords);
            }
            return outTitle;
        }
    }
}