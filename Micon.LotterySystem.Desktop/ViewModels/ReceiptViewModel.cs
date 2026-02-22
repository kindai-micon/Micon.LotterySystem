using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micon.LotterySystem.Desktop.Models;
using Micon.LotterySystem.Desktop.Services;

namespace Micon.LotterySystem.Desktop.ViewModels;

public partial class ReceiptViewModel : ViewModelBase
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private int _ticketCount = 1;

    [ObservableProperty]
    private string _lotteryGroupId = string.Empty;

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

    public string StatusHint => ActivateOnIssue
        ? "QRコード読み込み時すぐに使用可能になります"
        : "管理者による有効化が必要です";

    public ReceiptViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    partial void OnActivateOnIssueChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusHint));
    }

    [RelayCommand]
    private async Task PrintTickets()
    {
        if (string.IsNullOrWhiteSpace(LotteryGroupId))
        {
            StatusMessage = "抽選会IDを入力してください";
            return;
        }

        if (!Guid.TryParse(LotteryGroupId, out var groupId))
        {
            StatusMessage = "抽選会IDの形式が正しくありません";
            return;
        }

        if (TicketCount < 1 || TicketCount > 100)
        {
            StatusMessage = "発行枚数は1〜100で指定してください";
            return;
        }

        IsLoading = true;
        PrintProgress = 0;
        PrintTotal = TicketCount;

        try
        {
            // 1. JSONでチケット情報を取得
            StatusMessage = "チケット情報を取得中...";
            var result = await _apiService.IssueTicketsAsync(TicketCount, groupId);

            if (!string.IsNullOrEmpty(result.Error))
            {
                StatusMessage = $"エラー: {result.Error}";
                return;
            }

            // 2. 各チケットを印刷
            foreach (var ticket in result.Tickets)
            {
                PrintProgress++;

                // 2-1. QRコード画像を取得
                StatusMessage = $"QRコード取得中... ({PrintProgress}/{PrintTotal})";
                var qrCodeImage = await _apiService.GetQrCodeAsync(ticket.DisplayId);

                if (qrCodeImage == null)
                {
                    StatusMessage = $"QRコードの取得に失敗しました (番号: {ticket.Number})";
                    return;
                }

                // 2-2. 印刷処理（未実装）
                // TODO: レシートプリンタへの印刷処理を実装
                // PrintReceipt(ticket, qrCodeImage);

                // 2-3. Complete送信
                StatusMessage = $"完了処理中... ({PrintProgress}/{PrintTotal})";
                var completeResult = await _apiService.CompleteTicketAsync(ticket.DisplayId, ActivateOnIssue);

                if (!completeResult.Success)
                {
                    StatusMessage = $"完了処理エラー (番号: {ticket.Number}): {completeResult.Error}";
                    return;
                }
            }

            StatusMessage = $"{result.Tickets.Count}枚のチケットを印刷しました";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearTickets()
    {
        LotteryGroupId = string.Empty;
        TicketCount = 1;
        StatusMessage = string.Empty;
        PrintProgress = 0;
        PrintTotal = 0;
    }
}
