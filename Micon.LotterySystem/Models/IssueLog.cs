using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models
{
    public class IssueLog : BaseModel
    {
        // BaseModel が Guid Id, Created, Updated を提供
        public string IssuerName { get; set; } = string.Empty;
        public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
        public int Count { get; set; }
        public long StartNumber { get; set; }
        public long EndNumber { get; set; }
        // 抽選会ID（DisplayId）でログを紐づけ
        public Guid LotteryGroupDisplayId { get; set; }
    }
}
