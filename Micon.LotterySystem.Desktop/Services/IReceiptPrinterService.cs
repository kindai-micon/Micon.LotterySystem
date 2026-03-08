using System.Threading;
using System.Threading.Tasks;
using Micon.LotterySystem.Desktop.Models;

namespace Micon.LotterySystem.Desktop.Services;

public interface IReceiptPrinterService
{
    Task<PrintResult> ValidatePrinterAsync(string? printerName = null, CancellationToken cancellationToken = default);

    Task<PrintResult> PrintAsync(ReceiptPrintJob printJob, CancellationToken cancellationToken = default);
}