namespace Micon.LotterySystem.Models.API
{
    /// <summary>
    /// TicketInfo API応答用DTO
    /// </summary>
    public class TicketInfoResponse
    {
        public string Name { get; set; } = "";
        public string TicketLabel { get; set; } = "";
        public string Description { get; set; } = "";
        public string Warning { get; set; } = "";
        public string WarningText { get; set; } = "";
        public string? FooterText { get; set; }
    }
}
