namespace Micon.LotterySystem.Models
{
    public class LotteryGroup:BaseModel
    {
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<Ticket> Tickets { get; set; }
        public List<LotterySlots> LotterySlots { get; set; }
    }
}
