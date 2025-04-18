using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Micon.LotterySystem.Models;

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
            // DisplayId から LotteryGroup レコードを取得
            var group = await _db.LotteryGroups
                                 .FirstOrDefaultAsync(g => g.DisplayId == lotteryGroupDisplayId);

            if (group == null)
                return NotFound($"抽選会 {lotteryGroupDisplayId} が見つかりません");

            // 本来の PK (group.Id) でチケットを検索
            var tickets = await _db.Tickets
                .Where(t => t.LotteryGroupId == group.Id)
                .Select(t => new {
                    id = t.Id,
                    number = t.Number,
                    displayId = t.DisplayId,
                    status = t.Status.ToString(),
                    issuedAt = t.Created,
                    updatedAt = t.Updated
                })
                .OrderBy(t => t.number)
                .ToListAsync();

            return Ok(tickets);
        }
    }
}
