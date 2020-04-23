using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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


        public FileType ImpliedFileType(string path)
        {
            var extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
                return FileType.Other;
            if (VideoExtensions.Contains(extension))
                return FileType.Video;
            if (AudioExtensions.Contains(extension))
                return FileType.Audio;

            return FileType.Other;
        }

        public string GenerateFullLocalPath(MediaInformationContract contract)
        {
            var paths = new List<string>();
            paths.Add(_config["Directories:LocalBase"]);

            if (string.IsNullOrEmpty(contract.Container))
                throw new ArgumentNullException("All media types require container");

            switch (contract.Type)
            {
                case MediaType.Episode:
                    if (contract.Title.Count == 0 ||
                        !contract.Season.HasValue ||
                        !contract.Episode.HasValue)
                        throw new ArgumentNullException("Series must have a title, season, and episode");
                    paths.Add("tv");
                    paths.Add(string.Join(" - ", contract.Title));
                    paths.Add(string.Format("Season {0}", contract.Season));
                    paths.Add(string.Format("S{0:00}E{1:00}.{2}",
                        contract.Season, contract.Episode, contract.Container));
                    break;
                case MediaType.Movie:
                    if (contract.Title.Count == 0 ||
                        !contract.Year.HasValue)
                        throw new ArgumentNullException("Movies must have a title and year");
                    paths.Add("movies");
                    var title = string.Join(" - ", contract.Title);
                    if (!string.IsNullOrEmpty(contract.AlternativeTitle))
                        title += string.Format(" - {0}");
                    title += string.Format(" ({0}).{1}", contract.Year, contract.Container);
                    paths.Add(title);
                    break;
                default:
                    throw new NotImplementedException("Media type not implemented");
            }

            return Path.Combine(paths.ToArray());
        }
    }
}