using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models
{
    public class LotteryGroup:BaseModel
    {
        public string Name { get; set; }
        public List<Ticket> Tickets { get; set; }
        public List<LotterySlots> LotterySlots { get; set; }
        [ForeignKey(nameof(TicketInfo))]
        public Guid TicketInfoId { get; set; }
        public TicketInfo TicketInfo { get; set; } 
    }
}
