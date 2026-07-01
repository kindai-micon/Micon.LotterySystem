namespace Micon.LotterySystem.Models.API
{
    public class WinningModel
    {
        public string SlotId {  get; set; }
        public string Name { get; set; }
        public List<WinnerTicket> Tickets { get; set; } = new List<WinnerTicket>();
        public SlotStatus Status { get; set; } = SlotStatus.BeforeTheLottery;
        public int NumberOfFrames { get; set; }
    }
    public class WinnerTicket
    {
        public string Number { get; set; }
        public TicketStatus Status { get; set; }
    }
}
