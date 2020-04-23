using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sshanty.Contracts.Enums;

namespace Sshanty.Helpers
{
    public static class FileNameHelper
    {
        public static string[] VideoExtensions = new[] { ".mkv", ".mp4", ".avi", ".m4v", ".webm" };
        public static string[] AudioExtensions = new[] { ".mp3", ".m4a", ".flac", ".alac", ".aac", ".weba", ".ogg", ".wav" };

        public static FileType ImpliedFileType(string path)
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
    }
}