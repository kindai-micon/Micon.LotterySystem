using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Micon.LotterySystem.Models
{
    public class TicketInfo : BaseModel
    {
        public string Name { get; set; } = "抽選券";
        public string Description { get; set; } = "2025年度文化会新入生歓迎会";
        // 新規追加（DB保存対象）
        public string TicketLabel { get; set; } = "抽選券";
        public string? Warning { get; set; }
        public string? FooterText { get; set; }

        // ヘルパー（改行区切り文字列として扱う - NotMapped）
        [NotMapped]
        public string WarningText
        {
            get => Warning ?? "";
            set => Warning = value;
        }
    }
}
