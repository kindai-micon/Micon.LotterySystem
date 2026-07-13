using System;
using System.Collections.Generic;

namespace Micon.LotterySystem.Desktop.Models;

/// <summary>
/// 発行した抽選券の記録
/// </summary>
public class IssuedTicketRecord
{
    public Guid DisplayId { get; set; }
    public long Number { get; set; }
    public string Status { get; set; } = "PrintPublishing";
    public DateTime IssuedAt { get; set; }
    public string LotteryGroupName { get; set; } = "";
    public Guid LotteryGroupDisplayId { get; set; }
}

/// <summary>
/// 抽選会ごとの発行記録
/// </summary>
public class LotteryGroupRecords
{
    public Guid LotteryGroupDisplayId { get; set; }
    public string LotteryGroupName { get; set; } = "";
    public List<IssuedTicketRecord> Tickets { get; set; } = [];
}
