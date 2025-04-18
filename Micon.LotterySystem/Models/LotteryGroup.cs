using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models
{
    public class LotteryGroup:BaseModel
    {
        public Guid DisplayId { get; set; } = Guid.CreateVersion7();

        public string Name { get; set; }
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();
        public List<LotterySlots> LotterySlots { get; set; } = new List<LotterySlots>();
        [ForeignKey(nameof(TicketInfo))]
        public Guid TicketInfoId { get; set; }
        public TicketInfo TicketInfo { get; set; } 
    }
}
