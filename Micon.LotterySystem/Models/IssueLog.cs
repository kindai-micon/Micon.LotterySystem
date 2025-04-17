using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micon.LotterySystem.Models
{
    public class IssueLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string IssuerName { get; set; }
        public DateTime IssuedAt { get; set; }
        public int Count { get; set; }
        public long StartNumber { get; set; }
        public long EndNumber { get; set; }

        // 抽選会ID（DisplayId）でログを紐づけ
        public Guid LotteryGroupDisplayId { get; set; }
    }
}