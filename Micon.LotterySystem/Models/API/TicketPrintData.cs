namespace Micon.LotterySystem.Models.API
{
    /// <summary>
    /// チケット印刷・PDF生成用のデータ転送オブジェクト
    /// ※ DBには保存しない一時的なデータ
    /// </summary>
    public class TicketPrintData
    {
        public long TicketNumber { get; set; }
        public Guid DisplayId { get; set; }
        public string Url { get; set; } = "";

        // 表示用テキスト（TicketInfoから取得）
        public string LotteryGroupName { get; set; } = "";
        public string TicketLabel { get; set; } = "抽選券";
        public string? Description { get; set; }
        public string WarningText { get; set; } = "";
        public string? FooterText { get; set; }
    }
}
