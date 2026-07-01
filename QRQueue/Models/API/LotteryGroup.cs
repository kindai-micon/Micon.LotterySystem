using System.ComponentModel.DataAnnotations.Schema;

namespace QRQueue.Models.API
{
    public class LotteryGroup
    {
        public string Name { get; set; }
        public List<Ticket> Tickets { get; set; }
        public List<LotterySlots> LotterySlots { get; set; }
        public Guid TicketInfoId { get; set; }
        public TicketInfo TicketInfo { get; set; }
    }
}
