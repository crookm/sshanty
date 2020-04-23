using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sshanty.Contracts.Enums;
using Sshanty.Helpers;

namespace Sshanty.Contracts
{
    public class MediaInformationContract
    {
        [JsonConverter(typeof(JsonOptionalStringToListConverter))]
        public List<string> Title { get; set; }
        public int? Year { get; set; }
        public MediaType? Type { get; set; }
        public int? Episode { get; set; }
        public int? Season { get; set; }
        public bool Success { get; set; } = false;
    }
}