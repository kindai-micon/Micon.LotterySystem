using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Micon.LotterySystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace Micon.LotterySystem.Controllers
{
    [ApiController]
    [Route("api/ticket")]
    public class TicketController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TicketController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("activate/{guid}")]
        public async Task<IActionResult> Activate(Guid guid)
        {
            var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.DisplayId == guid);
            if (ticket == null)
                return NotFound("チケットが見つかりません");

            if (ticket.Status == TicketStatus.Valid)
                return Ok("すでに有効化されています");

            ticket.Status = TicketStatus.Valid;
            ticket.Updated = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            return Ok("チケットを有効化しました");
        }

        [HttpPost("deactivate/{guid}")]
        public async Task<IActionResult> Deactivate(Guid guid)
        {
            var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.DisplayId == guid);
            if (ticket == null)
                return NotFound("チケットが見つかりません");

            if (ticket.Status == TicketStatus.Invalid)
                return Ok("すでに無効化されています");

            ticket.Status = TicketStatus.Invalid;
            ticket.Updated = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            return Ok("チケットを無効化しました");
        }

        [HttpGet("{guid}")]
        public async Task<IActionResult> GetStatus(Guid guid)
        {
            var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.DisplayId == guid);
            if (ticket == null)
                return NotFound("チケットが見つかりません");

            return Ok(new
            {
                number = ticket.Number,
                status = ticket.Status.ToString()
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetTickets([FromQuery] Guid lotteryGroupDisplayId)
        {
            // DisplayId から LotteryGroup を取得
            var group = await _db.LotteryGroups
                                 .FirstOrDefaultAsync(g => g.DisplayId == lotteryGroupDisplayId);
            if (group == null)
                return NotFound();

            // チケットと発行ログを内部結合して issuerName を取得
            var tickets = await _db.Tickets
                .Where(t => t.LotteryGroupId == group.Id)
                .Select(t => new {
                    number = t.Number,
                    status = t.Status.ToString(),
                    issuedAt = t.Created,
                    updatedAt = t.Updated,
                    issuerName = _db.IssueLogs
                        .Where(log => log.LotteryGroupDisplayId == lotteryGroupDisplayId
                                   && t.Number >= log.StartNumber
                                   && t.Number <= log.EndNumber)
                        .Select(log => log.IssuerName)
                        .FirstOrDefault() ?? "—"   // 見つからなければダッシュ
                })
                .OrderBy(x => x.number)
                .ToListAsync();

            return Ok(tickets);
        }

    }
}
