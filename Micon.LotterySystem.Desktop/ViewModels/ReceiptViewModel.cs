using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micon.LotterySystem.Desktop.Models;
using Micon.LotterySystem.Desktop.Services;

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

    public ReceiptViewModel(IApiService apiService, ILocalStorageService localStorage, LotteryGroupInfo lotteryGroup)
    {
        _apiService = apiService;
        _localStorage = localStorage;
        _lotteryGroup = lotteryGroup;

        // ローカルから履歴を読み込む
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
        PrintTotal = TicketCount;

        var newRecords = new List<IssuedTicketRecord>();
        var failedTickets = new List<FailedTicketInfo>();

        try
        {
            // 1. JSONでチケット情報を取得
            StatusMessage = "チケット情報を取得中...";
            var result = await _apiService.IssueTicketsAsync(TicketCount, LotteryGroup.DisplayId);

            if (!string.IsNullOrEmpty(result.Error))
            {
                StatusMessage = $"エラー: {result.Error}";
                return;
            }

            // 2. 各チケットを処理
            foreach (var ticket in result.Tickets)
            {
                // ローカル記録を作成（初期状態はPrintPublishing）
                var record = new IssuedTicketRecord
                {
                    DisplayId = ticket.DisplayId,
                    Number = ticket.Number,
                    Status = "PrintPublishing",
                    IssuedAt = DateTime.Now,
                    LotteryGroupName = LotteryGroup.Name,
                    LotteryGroupDisplayId = LotteryGroup.DisplayId
                };
                newRecords.Add(record);

                // 2-1. QRコード画像を取得
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PrintProgress++;
                    StatusMessage = $"QRコード取得中... ({PrintProgress}/{PrintTotal})";
                });

                var qrCodeImage = await _apiService.GetQrCodeAsync(ticket.DisplayId);

                if (qrCodeImage == null)
                {
                    // QRコード取得失敗を記録
                    failedTickets.Add(new FailedTicketInfo
                    {
                        Number = ticket.Number,
                        DisplayId = ticket.DisplayId,
                        Reason = "QRコードの取得に失敗しました"
                    });

                    // ローカルには保存（PrintPublishingのまま）
                    await Dispatcher.UIThread.InvokeAsync(() => IssuedTickets.Insert(0, record));
                    _localStorage.AddRecords(LotteryGroup.DisplayId, LotteryGroup.Name, new[] { record });
                    continue;
                }

                // 2-2. 印刷処理（未実装）
                // TODO: レシートプリンタへの印刷処理を実装
                // PrintReceipt(ticket, qrCodeImage);

                // 2-3. Complete送信
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"完了処理中... ({PrintProgress}/{PrintTotal})";
                });

                var completeResult = await _apiService.CompleteTicketAsync(ticket.DisplayId, ActivateOnIssue);

                // Complete結果に応じて状態を更新
                if (completeResult.Success)
                {
                    record.Status = ActivateOnIssue ? "Valid" : "Invalid";
                }
                else
                {
                    // Complete失敗を記録
                    failedTickets.Add(new FailedTicketInfo
                    {
                        Number = ticket.Number,
                        DisplayId = ticket.DisplayId,
                        Reason = completeResult.Error ?? "完了処理に失敗しました"
                    });
                }

                // UIに追加
                await Dispatcher.UIThread.InvokeAsync(() => IssuedTickets.Insert(0, record));

                // ローカルに保存
                _localStorage.AddRecords(LotteryGroup.DisplayId, LotteryGroup.Name, new[] { record });
            }

            // 完了メッセージ
            if (failedTickets.Count > 0)
            {
                StatusMessage = $"{result.Tickets.Count}枚中{failedTickets.Count}枚の処理に失敗しました";
            }
            else
            {
                StatusMessage = $"{result.Tickets.Count}枚のチケットを印刷しました";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsLoading = false;

            // 失敗したチケットがあればダイアログ表示
            if (failedTickets.Count > 0)
            {
                ShowFailedTicketsDialog?.Invoke(failedTickets);
            }
        }
    }

    [RelayCommand]
    private void Clear()
    {
        TicketCount = 1;
        StatusMessage = string.Empty;
        PrintProgress = 0;
        PrintTotal = 0;

        // 履歴をクリア
        _localStorage.ClearRecords(LotteryGroup.DisplayId);
        IssuedTickets.Clear();
    }

    [RelayCommand]
    private void Back()
    {
        BackRequested?.Invoke();
    }
}
