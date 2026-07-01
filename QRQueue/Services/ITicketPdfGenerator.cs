using QRQueue.Models;

namespace QRQueue.Services
{
    public interface ITicketPdfGenerator
    {
        byte[] GenerateTicketsPdf(List<TicketInfo> tickets);
    }
}
