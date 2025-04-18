using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models
{
    public class TicketInfo : BaseModel
    {
        public string Name { get; set; } = "抽選券";
        public string Description { get; set; } = "2025年度文化会新入生歓迎会";
        public string Warning { get; set; } = "当日のみ有効 本券は汚したり破らないよう大切に保管してください";

        // ↓ 以下はDBには保存しないプロパティ
        [NotMapped]
        public int TicketNumber { get; set; }

        [NotMapped]
        public Guid Guid { get; set; }

        [NotMapped]
        public const string BaseUrl = $"https://lottery.micon.culb/ticket/";

        [NotMapped]
        public string Url { get; set; }
    }
}
