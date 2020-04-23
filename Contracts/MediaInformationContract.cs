using Sshanty.Contracts.Enums;

namespace Sshanty.Contracts
{
    public class MediaInformationContract
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public MediaType? Type { get; set; }
        public int? Episode { get; set; }
        public int? Season { get; set; }
        public bool Success { get; set; } = false;
    }
}