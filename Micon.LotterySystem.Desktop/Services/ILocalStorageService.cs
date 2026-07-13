using System;
using System.Collections.Generic;
using Micon.LotterySystem.Desktop.Models;

namespace Micon.LotterySystem.Desktop.Services;

public interface ILocalStorageService
{
    /// <summary>
    /// 指定した抽選会の発行記録を取得
    /// </summary>
    List<IssuedTicketRecord> GetRecords(Guid lotteryGroupDisplayId);

    /// <summary>
    /// 発行記録を追加
    /// </summary>
    void AddRecords(Guid lotteryGroupDisplayId, string lotteryGroupName, IEnumerable<IssuedTicketRecord> records);

    /// <summary>
    /// チケットの状態を更新
    /// </summary>
    void UpdateTicketStatus(Guid displayId, string newStatus);

    /// <summary>
    /// 全ての記録を取得（抽選会一覧用）
    /// </summary>
    List<LotteryGroupRecords> GetAllRecords();

    /// <summary>
    /// 指定した抽選会の記録を削除
    /// </summary>
    void ClearRecords(Guid lotteryGroupDisplayId);
}
