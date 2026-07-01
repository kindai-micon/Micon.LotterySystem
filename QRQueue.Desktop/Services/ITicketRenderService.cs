using QRQueue.Desktop.Models;

namespace QRQueue.Desktop.Services;

public interface ITicketRenderService
{
    byte[] RenderEscPos(ReceiptPrintJob printJob);
}