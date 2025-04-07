using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models
{
    public class Ticket : BaseModel
    {
        public string Number { get; set; }
        [ForeignKey(nameof(LotteryGroup))]
        public Guid LotteryGroupId { get; set; }
        public LotteryGroup LotteryGroup { get; set; }
        [ForeignKey(nameof(LotterySlots))]
        public Guid? LotterySlotsId { get; set; } = null;
        public LotterySlots? LotterySlots { get; set; } = null;
        public TicketStatus Status { get; set; } = TicketStatus.Invalid;
    }
    public enum TicketStatus
    {
        Invalid,
        Valid,
        Winner
    }
}
