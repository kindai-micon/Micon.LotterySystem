using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micon.LotterySystem.Desktop.Models;
using Micon.LotterySystem.Desktop.Services;
using Micon.LotterySystem.Desktop.Settings;

namespace Micon.LotterySystem.Desktop.ViewModels;

public class FailedTicketInfo
{
    public long Number { get; set; }
    public Guid DisplayId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public partial class ReceiptViewModel : ViewModelBase
{
    private readonly IApiService _apiService;
    private readonly ILocalStorageService _localStorage;
    private readonly IReceiptPrinterService _receiptPrinterService;
    private readonly PrinterSettings _printerSettings;
    private readonly ReceiptLayoutSettings _receiptLayoutSettings;

    [ObservableProperty]
    private int _ticketCount = 1;

    [ObservableProperty]
    private LotteryGroupInfo _lotteryGroup;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _activateOnIssue = true;

    [ObservableProperty]
    private int _printProgress;

    [ObservableProperty]
    private int _printTotal;

    public string LotteryGroupName => LotteryGroup?.Name ?? "";

    public ObservableCollection<IssuedTicketRecord> IssuedTickets { get; } = [];

    public string StatusHint => ActivateOnIssue
        ? "QRコード読み込み時すぐに使用可能になります"
        : "管理者による有効化が必要です";

    public event Action? BackRequested;
    public event Action<List<FailedTicketInfo>>? ShowFailedTicketsDialog;

    public ReceiptViewModel(
        IApiService apiService,
        ILocalStorageService localStorage,
        IReceiptPrinterService receiptPrinterService,
        PrinterSettings printerSettings,
        ReceiptLayoutSettings receiptLayoutSettings,
        LotteryGroupInfo lotteryGroup)
    {
        _apiService = apiService;
        _localStorage = localStorage;
        _receiptPrinterService = receiptPrinterService;
        _printerSettings = printerSettings;
        _receiptLayoutSettings = receiptLayoutSettings;
        _lotteryGroup = lotteryGroup;

        LoadLocalRecords();
    }

    private void LoadLocalRecords()
    {
        var records = _localStorage.GetRecords(LotteryGroup.DisplayId);
        IssuedTickets.Clear();
        foreach (var record in records)
        {
            IssuedTickets.Add(record);
        }
    }

    partial void OnActivateOnIssueChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusHint));
    }

    partial void OnLotteryGroupChanged(LotteryGroupInfo value)
    {
        OnPropertyChanged(nameof(LotteryGroupName));
    }

    [RelayCommand]
    private async Task PrintTickets()
    {
        if (LotteryGroup == null)
        {
            StatusMessage = "抽選会が選択されていません";
            return;
        }

        if (TicketCount < 1 || TicketCount > 1000)
        {
            StatusMessage = "発行枚数は1〜1000で指定してください";
            return;
        }

        IsLoading = true;
        PrintProgress = 0;
        PrintTotal = 0;

        var failedTickets = new List<FailedTicketInfo>();
        var successCount = 0;
        var qrFailedCount = 0;
        var printFailedCount = 0;
        var completeFailedCount = 0;

        try
        {
            StatusMessage = "プリンタ接続を確認中...";
            var printerCheckResult = await _receiptPrinterService.ValidatePrinterAsync(_printerSettings.PrinterName);

            if (!printerCheckResult.IsSuccess)
            {
                StatusMessage = $"印刷前チェックに失敗しました: {printerCheckResult.Message}";
                return;
            }

            StatusMessage = "チケット情報を取得中...";
            var result = await _apiService.IssueTicketsAsync(TicketCount, LotteryGroup.DisplayId);

            if (!string.IsNullOrEmpty(result.Error))
            {
                StatusMessage = $"エラー: {result.Error}";
                return;
            }

            if (result.Tickets.Count == 0)
            {
                StatusMessage = "発行対象のチケットがありませんでした";
                return;
            }

            var lotteryGroupName = string.IsNullOrWhiteSpace(result.LotteryGroupName)
                ? LotteryGroup.Name
                : result.LotteryGroupName;

            PrintTotal = result.Tickets.Count;

            foreach (var ticket in result.Tickets)
            {
                var record = new IssuedTicketRecord
                {
                    DisplayId = ticket.DisplayId,
                    Number = ticket.Number,
                    Status = "PrintPublishing",
                    IssuedAt = ticket.IssuedAt.LocalDateTime,
                    LotteryGroupName = lotteryGroupName,
                    LotteryGroupDisplayId = LotteryGroup.DisplayId
                };

                PrintProgress++;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"QRコード取得中... ({PrintProgress}/{PrintTotal}) No.{ticket.Number}";
                });

                var qrCodeImage = await _apiService.GetQrCodeAsync(ticket.DisplayId);

                if (qrCodeImage == null)
                {
                    qrFailedCount++;

                    failedTickets.Add(new FailedTicketInfo
                    {
                        Number = ticket.Number,
                        DisplayId = ticket.DisplayId,
                        Reason = "QRコードの取得に失敗しました"
                    });

                    await AddRecordAsync(record);
                    continue;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"印刷中... ({PrintProgress}/{PrintTotal}) No.{ticket.Number}";
                });

                var printJob = new ReceiptPrintJob
                {
                    LotteryGroupName = lotteryGroupName,
                    TicketNumber = ticket.Number,
                    TicketDisplayId = ticket.DisplayId,
                    IssuedAt = ticket.IssuedAt,
                    QrCodePngBytes = qrCodeImage,
                    ActivateOnIssue = ActivateOnIssue,
                    PrinterName = _printerSettings.PrinterName,
                    WarningLines = _receiptLayoutSettings.WarningLines,
                    FooterText = _receiptLayoutSettings.FooterText
                };

                var printResult = await _receiptPrinterService.PrintAsync(printJob);

                if (!printResult.IsSuccess)
                {
                    printFailedCount++;

                    failedTickets.Add(new FailedTicketInfo
                    {
                        Number = ticket.Number,
                        DisplayId = ticket.DisplayId,
                        Reason = BuildPrintFailureReason(printResult)
                    });

                    await AddRecordAsync(record);
                    continue;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"状態更新中... ({PrintProgress}/{PrintTotal}) No.{ticket.Number}";
                });

                var completeResult = await _apiService.CompleteTicketAsync(ticket.DisplayId, ActivateOnIssue);

                if (completeResult.Success)
                {
                    successCount++;
                    record.Status = ActivateOnIssue ? "Valid" : "Invalid";
                }
                else
                {
                    completeFailedCount++;

                    var reason = completeResult.Error
                        ?? completeResult.Message
                        ?? "状態更新時に不明なエラーが発生しました";

                    failedTickets.Add(new FailedTicketInfo
                    {
                        Number = ticket.Number,
                        DisplayId = ticket.DisplayId,
                        Reason = $"印刷後の状態更新に失敗しました: {reason}"
                    });
                }

                await AddRecordAsync(record);
            }

            StatusMessage = BuildSummaryMessage(
                result.Tickets.Count,
                successCount,
                qrFailedCount,
                printFailedCount,
                completeFailedCount);
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsLoading = false;

            if (failedTickets.Count > 0)
            {
                ShowFailedTicketsDialog?.Invoke(failedTickets);
            }
        }
    }

    private async Task AddRecordAsync(IssuedTicketRecord record)
    {
        await Dispatcher.UIThread.InvokeAsync(() => IssuedTickets.Insert(0, record));
        _localStorage.AddRecords(LotteryGroup.DisplayId, LotteryGroup.Name, new[] { record });
    }

    private static string BuildPrintFailureReason(PrintResult printResult)
    {
        var prefix = printResult.Stage switch
        {
            PrintStage.QrFetch => "QRコード取得に失敗しました",
            PrintStage.Render => "券面生成に失敗しました",
            PrintStage.SendToPrinter => "プリンタ送信に失敗しました",
            PrintStage.Complete => "状態更新に失敗しました",
            _ => "印刷処理に失敗しました"
        };

        return string.IsNullOrWhiteSpace(printResult.Message)
            ? prefix
            : $"{prefix}: {printResult.Message}";
    }

    private static string BuildSummaryMessage(
        int totalCount,
        int successCount,
        int qrFailedCount,
        int printFailedCount,
        int completeFailedCount)
    {
        if (totalCount <= 0)
        {
            return "発行対象のチケットがありませんでした";
        }

        if (qrFailedCount == 0 && printFailedCount == 0 && completeFailedCount == 0)
        {
            return $"{totalCount}枚のチケットを印刷しました";
        }

        return
            $"{totalCount}枚中 " +
            $"成功{successCount}枚 / " +
            $"QR取得失敗{qrFailedCount}枚 / " +
            $"印刷失敗{printFailedCount}枚 / " +
            $"状態更新失敗{completeFailedCount}枚";
    }

    [RelayCommand]
    private void Clear()
    {
        TicketCount = 1;
        StatusMessage = string.Empty;
        PrintProgress = 0;
        PrintTotal = 0;

        _localStorage.ClearRecords(LotteryGroup.DisplayId);
        IssuedTickets.Clear();
    }

    [RelayCommand]
    private void Back()
    {
        BackRequested?.Invoke();
    }
}