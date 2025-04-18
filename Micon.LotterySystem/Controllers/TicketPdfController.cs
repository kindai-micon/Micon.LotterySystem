using Micon.LotterySystem.Models;
using Micon.LotterySystem;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Route("api/pdf")]
[ApiController]
public class TicketPdfController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private static List<IssueLog> _issueLogs = new();


    public TicketPdfController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GeneratePdf([FromBody] TicketRequest request)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var lotteryGroup = _db.LotteryGroups
            .Include(g => g.TicketInfo)
            .FirstOrDefault(g => g.DisplayId == request.LotteryGroupId);

        if (lotteryGroup == null || lotteryGroup.TicketInfo == null)
            return BadRequest("無効な抽選会IDまたはチケット情報が未設定です");


        long startNumber = _db.Tickets
            .Where(t => t.LotteryGroupId == lotteryGroup.Id)
            .Select(t => t.Number)
            .ToList()
            .DefaultIfEmpty(999)
            .Max() + 1;

        var tickets = new List<Ticket>();
        for (int i = 0; i < request.Count; i++)
        {
            tickets.Add(new Ticket
            {
                Number = startNumber + i,
                LotteryGroupId = lotteryGroup.Id,
                Status = TicketStatus.Invalid
            });
        }

        _db.Tickets.AddRange(tickets);
        await _db.SaveChangesAsync();

        var log = new IssueLog
        {
            IssuerName = user.UserName ?? "Unknown",
            IssuedAt = DateTime.UtcNow,
            Count = request.Count,
            StartNumber = startNumber,
            EndNumber = startNumber + request.Count - 1,
            LotteryGroupDisplayId = request.LotteryGroupId
        };

        _db.IssueLogs.Add(log);
        await _db.SaveChangesAsync();

        var ticketInfos = tickets.Select(t => new TicketInfo
        {
            TicketNumber = (int)t.Number,
            Guid = t.DisplayId,
            Name = lotteryGroup.TicketInfo?.Name ?? "抽選券",
            Description = lotteryGroup.TicketInfo?.Description ?? "",
            Warning = lotteryGroup.TicketInfo?.Warning ?? ""
        }).ToList();

        var generator = new TicketPdfGenerator();
        var bytes = generator.GenerateTicketsPdf(ticketInfos);

        return File(bytes, "application/pdf", "抽選券.pdf");
    }

    public class TicketRequest
    {
        public int Count { get; set; }
        public Guid LotteryGroupId { get; set; }
    }

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] Guid lotteryGroupId)
    {
        var logs = _db.IssueLogs
            .Where(log => log.LotteryGroupDisplayId == lotteryGroupId)
            .OrderByDescending(log => log.IssuedAt)
            .ToList();

        return Ok(logs);
    }
}

