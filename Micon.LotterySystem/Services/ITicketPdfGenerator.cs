using Micon.LotterySystem.Models.API;

namespace Micon.LotterySystem.Services
{
    public interface ITicketPdfGenerator
    {
        byte[] GenerateTicketsPdf(List<TicketPrintData> tickets);
    }
}
