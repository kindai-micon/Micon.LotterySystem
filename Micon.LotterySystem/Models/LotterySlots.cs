using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models
{
    public class LotterySlots:BaseModel
    {
        public Guid DisplayId { get; set; } = Guid.CreateVersion7();
        public string Name { get; set; }
        public string Merchandise { get; set; }
        public int NumberOfFrames { get; set; }
        public DateTimeOffset? DeadLine { get; set; }
        [ForeignKey(nameof(LotteryGroup))]
        public Guid LotteryGroupId { get; set; }
        public LotteryGroup LotteryGroup { get; set; }
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();
        public SlotStatus Status { get; set; } = SlotStatus.BeforeTheLottery;
    　　public int Order { get; set; } = 0;
    }
    public enum SlotStatus
    {
        BeforeTheLottery,
        TargetLottery,
        DuringAnimation,
        ViewResult,
        Exchange,
        StopExchange,
    }
}
