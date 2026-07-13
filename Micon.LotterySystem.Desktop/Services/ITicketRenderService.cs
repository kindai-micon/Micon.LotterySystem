using Micon.LotterySystem.Desktop.Models;

namespace Micon.LotterySystem.Desktop.Services;

public interface ITicketRenderService
{
    byte[] RenderEscPos(ReceiptPrintJob printJob);
}