using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models
{
    public class TicketInfo:BaseModel
    {
        public LotteryGroup LotteryGroup { get; set; }
        public string Name { get; set; } = "抽選券";
        public string Description { get; set; }
        public string Warning { get; set; }
    }
}
