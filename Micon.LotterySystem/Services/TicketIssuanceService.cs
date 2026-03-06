using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Micon.LotterySystem.Models;

namespace Micon.LotterySystem.Services;

public interface ITicketIssuanceService
{
    /// <summary>
    /// チケットを発行し、ログを記録する
    /// </summary>
    /// <param name="lotteryGroupDisplayId">抽選会のDisplayId</param>
    /// <param name="count">発行枚数</param>
    /// <param name="initialStatus">初期ステータス</param>
    /// <param name="issuerName">発行者名</param>
    /// <returns>発行結果</returns>
    Task<TicketIssuanceResult> IssueTicketsAsync(
        Guid lotteryGroupDisplayId,
        int count,
        TicketStatus initialStatus,
        string issuerName);
}

public class TicketIssuanceResult
{
    public List<Ticket> Tickets { get; set; } = [];
    public long StartNumber { get; set; }
    public long EndNumber { get; set; }
    public Guid LotteryGroupId { get; set; }
    public string? LotteryGroupName { get; set; }
}

public class TicketIssuanceService : ITicketIssuanceService
{
    private readonly ApplicationDbContext _db;

    public TicketIssuanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TicketIssuanceResult> IssueTicketsAsync(
        Guid lotteryGroupDisplayId,
        int count,
        TicketStatus initialStatus,
        string issuerName)
    {
        // バリデーション
        if (count <= 0)
        {
            throw new ArgumentException("発行枚数は1以上である必要があります", nameof(count));
        }

        if (count > 1000)
        {
            throw new ArgumentException("一度に発行できるチケットは最大1000枚です", nameof(count));
        }

        // トランザクション内で実行
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        try
        {
            // 抽選会を取得
            var lotteryGroup = await _db.LotteryGroups
                .FirstOrDefaultAsync(g => g.DisplayId == lotteryGroupDisplayId);

            if (lotteryGroup == null)
            {
                throw new InvalidOperationException("抽選会が見つかりません");
            }

            // 次のチケット番号を取得（Serializable分離レベルで競合を防止）
            var maxNumber = await _db.Tickets
                .Where(t => t.LotteryGroupId == lotteryGroup.Id)
                .MaxAsync(t => (long?)t.Number);
            long startNumber = (maxNumber ?? 999) + 1;

            // チケットを作成
            var tickets = new List<Ticket>();
            for (int i = 0; i < count; i++)
            {
                tickets.Add(new Ticket
                {
                    Number = startNumber + i,
                    LotteryGroupId = lotteryGroup.Id,
                    Status = initialStatus
                });
            }

            _db.Tickets.AddRange(tickets);

            // 発行ログを保存
            var log = new IssueLog
            {
                IssuerName = issuerName,
                IssuedAt = DateTime.UtcNow,
                Count = count,
                StartNumber = startNumber,
                EndNumber = startNumber + count - 1,
                LotteryGroupDisplayId = lotteryGroupDisplayId
            };

            _db.IssueLogs.Add(log);

            // チケットとログを一つのトランザクションで保存
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new TicketIssuanceResult
            {
                Tickets = tickets,
                StartNumber = startNumber,
                EndNumber = startNumber + count - 1,
                LotteryGroupId = lotteryGroup.Id,
                LotteryGroupName = lotteryGroup.Name
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
