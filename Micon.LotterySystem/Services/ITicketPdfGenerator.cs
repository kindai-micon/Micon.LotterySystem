using Micon.LotterySystem.Models;

namespace Micon.LotterySystem.Services
{
    public interface ITicketPdfGenerator
    {
        byte[] GenerateTicketsPdf(List<TicketInfo> tickets);
    }
}
