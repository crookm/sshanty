using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sshanty.Contracts;
using Sshanty.Services;

namespace Sshanty.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaInformationController : ControllerBase
    {
        private readonly ILogger<MediaInformationController> _logger;
        private readonly MediaFileService _mediaFileService;
        private readonly MediaInformationService _mediaInformationService;

        public MediaInformationController(ILogger<MediaInformationController> logger, MediaFileService mediaFileService, MediaInformationService mediaInformationService)
        {
            _logger = logger;
            _mediaFileService = mediaFileService;
            _mediaInformationService = mediaInformationService;
        }

        [HttpGet]
        [Route("divine/mediainfo")]
        public MediaInformationContract MediaInformation([Required][FromQuery] string fileName)
        {
            return _mediaInformationService.GetMediaInformation(fileName);
        }

        [HttpGet]
        [Route("divine/localpath")]
        public string LocalPath([Required][FromQuery]string fileName)
        {
            var contract = _mediaInformationService.GetMediaInformation(fileName);
            return _mediaFileService.GenerateFullLocalPath(contract).FullName;
        }
    }
}
