using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models.API
{
    public class LotterySlots
    {
        public string LotteryId { get; set; }
        public string? SlotId { get; set; }
        public string? Name { get; set; }
        public string? Merchandise { get; set; }
        public int NumberOfFrames { get; set; }
        public DateTimeOffset? DeadLine { get; set; }
    }
}
