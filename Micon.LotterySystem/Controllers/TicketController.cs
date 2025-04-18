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
    }
}
