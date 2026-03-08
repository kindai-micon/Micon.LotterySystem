using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Micon.LotterySystem.Desktop.Models;
using Micon.LotterySystem.Desktop.Settings;

namespace Micon.LotterySystem.Desktop.Services;

public class ReceiptPrinterService : IReceiptPrinterService
{
    private readonly ITicketRenderService _ticketRenderService;
    private readonly IWinRawPrinter _winRawPrinter;
    private readonly PrinterSettings _printerSettings;
    private readonly ILogger<ReceiptPrinterService> _logger;

    public ReceiptPrinterService(
        ITicketRenderService ticketRenderService,
        IWinRawPrinter winRawPrinter,
        PrinterSettings printerSettings,
        ILogger<ReceiptPrinterService> logger)
    {
        _ticketRenderService = ticketRenderService;
        _winRawPrinter = winRawPrinter;
        _printerSettings = printerSettings;
        _logger = logger;
    }

    public Task<PrintResult> PrintAsync(ReceiptPrintJob printJob, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (printJob is null)
            {
                return Task.FromResult(PrintResult.Fail(
                    PrintStage.Render,
                    "印刷ジョブが存在しません。",
                    isPrinted: false,
                    canRetry: false));
            }

            var printerName = ResolvePrinterName(printJob);

            if (string.IsNullOrWhiteSpace(printerName))
            {
                return Task.FromResult(PrintResult.Fail(
                    PrintStage.SendToPrinter,
                    "プリンタ名が設定されていません。",
                    isPrinted: false,
                    canRetry: false));
            }

            if (!_winRawPrinter.CanOpen(printerName, out var openError))
            {
                return Task.FromResult(PrintResult.Fail(
                    PrintStage.SendToPrinter,
                    openError ?? $"プリンタ '{printerName}' を利用できません。",
                    isPrinted: false,
                    canRetry: true));
            }

            byte[] escPosBytes;
            try
            {
                escPosBytes = _ticketRenderService.RenderEscPos(printJob);
            }
            catch (TicketRenderException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Ticket render failed. TicketNumber={TicketNumber}, DisplayId={DisplayId}",
                    printJob.TicketNumber,
                    printJob.TicketDisplayId);

                return Task.FromResult(PrintResult.Fail(
                    PrintStage.Render,
                    ex.Message,
                    isPrinted: false,
                    canRetry: true));
            }

            try
            {
                _winRawPrinter.Print(printerName, _printerSettings.DocumentName, escPosBytes);
            }
            catch (RawPrinterException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Raw print failed. Printer={PrinterName}, TicketNumber={TicketNumber}, DisplayId={DisplayId}",
                    printerName,
                    printJob.TicketNumber,
                    printJob.TicketDisplayId);

                return Task.FromResult(PrintResult.Fail(
                    PrintStage.SendToPrinter,
                    ex.Message,
                    isPrinted: false,
                    canRetry: true));
            }

            _logger.LogInformation(
                "Receipt printing succeeded. Printer={PrinterName}, TicketNumber={TicketNumber}, DisplayId={DisplayId}",
                printerName,
                printJob.TicketNumber,
                printJob.TicketDisplayId);

            return Task.FromResult(PrintResult.Success("抽選券をプリンタへ送信しました。"));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Receipt printing was canceled. TicketNumber={TicketNumber}, DisplayId={DisplayId}",
                printJob?.TicketNumber,
                printJob?.TicketDisplayId);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected receipt printing error. TicketNumber={TicketNumber}, DisplayId={DisplayId}",
                printJob?.TicketNumber,
                printJob?.TicketDisplayId);

            return Task.FromResult(PrintResult.Fail(
                PrintStage.SendToPrinter,
                "印刷処理で予期しないエラーが発生しました。",
                isPrinted: false,
                canRetry: true));
        }
    }

    private string ResolvePrinterName(ReceiptPrintJob printJob)
    {
        if (!string.IsNullOrWhiteSpace(printJob.PrinterName))
        {
            return printJob.PrinterName.Trim();
        }

        return _printerSettings.PrinterName?.Trim() ?? string.Empty;
    }
}