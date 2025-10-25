using Micon.LotterySystem.Models;
using Micon.LotterySystem;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Micon.LotterySystem.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

[Route("api/pdf")]
[ApiController]
public class TicketPdfController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITicketPdfGenerator _pdfGenerator;
    private readonly IConfiguration _configuration;
    private readonly IServer _server;


    public TicketPdfController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ITicketPdfGenerator pdfGenerator, IConfiguration configuration, IServer server)
    {
        _db = db;
        _userManager = userManager;
        _pdfGenerator = pdfGenerator;
        _configuration = configuration;
        _server = server;
    }
    [Authorize(Policy = "TicketPublish")]

    [HttpPost("generate")]
    public async Task<IActionResult> GeneratePdf([FromBody] TicketRequest request)
    {
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

        // BaseURL の生成（設定 → 現在のリクエスト情報）
        string baseUrl = _configuration["LotteryBaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            var httpRequest = HttpContext.Request;

            // appsettings.jsonのUseHttpsForQrCode設定を使用、未設定の場合はリクエストのスキームを使用
            var useHttps = _configuration.GetValue<bool?>("UseHttpsForQrCode");
            var scheme = useHttps.HasValue
                ? (useHttps.Value ? "https" : "http")
                : httpRequest.Scheme;

            var host = httpRequest.Host.Host;

            // localhostの場合はローカルネットワークIPアドレスを取得
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                host = GetLocalIPAddress();
            }

            // 実際にアクセスされたポート番号を使用
            int? port = httpRequest.Host.Port;

            // ポート番号が取得できない場合は標準ポートを使用
            if (!port.HasValue)
            {
                port = scheme == "https" ? 443 : 80;
            }

            // 標準ポート（HTTP:80, HTTPS:443）以外の場合はポート番号を含める
            var portString = "";
            if ((scheme == "https" && port != 443) ||
                (scheme == "http" && port != 80))
            {
                portString = $":{port}";
            }

            baseUrl = $"{scheme}://{host}{portString}/ticket/";
        }
        else if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }

        var ticketInfo = tickets.Select(t => new TicketInfo
        {
            TicketNumber = (int)t.Number,
            Guid = t.DisplayId,
            Name = lotteryGroup.Name + " 抽選券",
            Description = lotteryGroup.Name,
            Warning = "当日のみ有効 本券は汚したり破らないよう大切に保管してください",
            Url = baseUrl + t.DisplayId.ToString()
        }).ToList();

        var bytes = _pdfGenerator.GenerateTicketsPdf(ticketInfo);

        return File(bytes, "application/pdf", "抽選券.pdf");
    }

    public class TicketRequest
    {
        public int Count { get; set; }
        public Guid LotteryGroupId { get; set; }
    }
    [Authorize]
    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] Guid lotteryGroupId)
    {
        var logs = _db.IssueLogs
            .Where(log => log.LotteryGroupDisplayId == lotteryGroupId)
            .OrderByDescending(log => log.IssuedAt)
            .ToList();

        return Ok(logs);
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "localhost"; // フォールバック
    }

}

