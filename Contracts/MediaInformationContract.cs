using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sshanty.Contracts.Enums;
using Sshanty.Serialisers;

namespace Sshanty.Contracts
{
    public class MediaInformationContract
    {
        [JsonConverter(typeof(JsonOptionalStringToListConverter))]
        public List<string> Title { get; set; }
        [JsonPropertyName("alternative_title")]
        public string AlternativeTitle { get; set; }
        public int? Year { get; set; }
        public MediaType? Type { get; set; }
        [JsonConverter(typeof(JsonOptionalIntToListConverter))]
        public List<int> Episode { get; set; }
        public int? Season { get; set; }
        public string Container { get; set; }
        public bool Success { get; set; } = false;

        public string GetEpisodeString()
        {
            return string.Join('+', Episode.Select(e => string.Format("E{0:00}", e)));
        }
    }
}