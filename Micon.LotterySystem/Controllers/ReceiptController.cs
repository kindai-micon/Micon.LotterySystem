using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Micon.LotterySystem.Models;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using Microsoft.Extensions.Configuration;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/receipt")]
    [ApiController]
    [Authorize(Policy = "ReceiptTicketPublish")]
    public class ReceiptController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public ReceiptController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _db = db;
            _userManager = userManager;
            _configuration = configuration;
        }

        /// <summary>
        /// 抽選会一覧取得
        /// </summary>
        [HttpGet("lottery-groups")]
        public async Task<IActionResult> GetLotteryGroups()
        {
            var groups = await _db.LotteryGroups
                .OrderByDescending(g => g.Created)
                .Select(g => new
                {
                    displayId = g.DisplayId,
                    name = g.Name
                })
                .ToListAsync();

            return Ok(groups);
        }

        /// <summary>
        /// チケットn枚発行（JSON形式）
        /// </summary>
        [HttpPost("tickets")]
        public async Task<IActionResult> IssueTickets([FromBody] IssueTicketsRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var lotteryGroup = await _db.LotteryGroups
                .Include(g => g.TicketInfo)
                .FirstOrDefaultAsync(g => g.DisplayId == request.LotteryGroupId);

            if (lotteryGroup == null)
                return NotFound(new { error = "抽選会が見つかりません" });

            // 次のチケット番号を取得
            long startNumber = await _db.Tickets
                .Where(t => t.LotteryGroupId == lotteryGroup.Id)
                .Select(t => t.Number)
                .DefaultIfEmpty(999)
                .MaxAsync() + 1;

            var tickets = new List<Ticket>();
            for (int i = 0; i < request.Count; i++)
            {
                tickets.Add(new Ticket
                {
                    Number = startNumber + i,
                    LotteryGroupId = lotteryGroup.Id,
                    Status = TicketStatus.PrintPublishing
                });
            }

            _db.Tickets.AddRange(tickets);
            await _db.SaveChangesAsync();

            // 発行ログを保存
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

            var baseUrl = GetBaseUrl();

            var response = new
            {
                tickets = tickets.Select(t => new
                {
                    displayId = t.DisplayId,
                    number = t.Number,
                    qrCodeUrl = $"/api/receipt/qrcode/{t.DisplayId}",
                    status = t.Status.ToString(),
                    issuedAt = t.Created
                }),
                lotteryGroupName = lotteryGroup.Name
            };

            return Ok(response);
        }

        /// <summary>
        /// QRコード画像取得
        /// ※ status="Invalid" のみ取得可能
        /// </summary>
        [HttpGet("qrcode/{displayId}")]
        public async Task<IActionResult> GetQrCode(Guid displayId)
        {
            var ticket = await _db.Tickets
                .Include(t => t.LotteryGroup)
                .FirstOrDefaultAsync(t => t.DisplayId == displayId);

            if (ticket == null)
                return NotFound(new { error = "チケットが見つかりません" });

            if (ticket.Status != TicketStatus.PrintPublishing)
                return Forbid();

            var baseUrl = GetBaseUrl();
            var qrUrl = $"{baseUrl}{ticket.DisplayId}";

            var qrCodeBytes = GenerateQrCode(qrUrl);

            return File(qrCodeBytes, "image/png");
        }

        /// <summary>
        /// チケット状態変更（印刷完了時など）
        /// </summary>
        [HttpPost("complete/{displayId}")]
        public async Task<IActionResult> CompleteTicket(Guid displayId, [FromBody] CompleteTicketRequest request)
        {
            var ticket = await _db.Tickets
                .Include(t => t.LotteryGroup)
                .FirstOrDefaultAsync(t => t.DisplayId == displayId);

            if (ticket == null)
                return NotFound(new { error = "チケットが見つかりません" });

            if (request.Activate)
            {
                if (ticket.Status == TicketStatus.Valid)
                    return Ok(new { success = true, status = ticket.Status.ToString(), message = "すでに有効化されています" });

                // PrintPublishing のみ有効化可能
                if (ticket.Status != TicketStatus.PrintPublishing)
                    return BadRequest(new { success = false, status = ticket.Status.ToString(), message = "この状態からは有効化できません" });

                ticket.Status = TicketStatus.Valid;
                ticket.Updated = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new { success = true, status = ticket.Status.ToString(), message = "チケットを有効化しました" });
            }
            else
            {
                if (ticket.Status == TicketStatus.Invalid)
                    return Ok(new { success = true, status = ticket.Status.ToString(), message = "すでに無効化されています" });

                // PrintPublishing のみ無効化可能
                if (ticket.Status != TicketStatus.PrintPublishing)
                    return BadRequest(new { success = false, status = ticket.Status.ToString(), message = "この状態からは無効化できません" });

                ticket.Status = TicketStatus.Invalid;
                ticket.Updated = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new { success = true, status = ticket.Status.ToString(), message = "チケットを無効化しました" });
            }
        }

        private string GetBaseUrl()
        {
            string baseUrl = _configuration["LotteryBaseUrl"] ?? "";

            if (string.IsNullOrEmpty(baseUrl))
            {
                var httpRequest = HttpContext.Request;
                var useHttps = _configuration.GetValue<bool?>("UseHttpsForQrCode");
                var scheme = useHttps.HasValue
                    ? (useHttps.Value ? "https" : "http")
                    : httpRequest.Scheme;

                var host = httpRequest.Host.Host;
                var port = httpRequest.Host.Port;

                var portString = "";
                if (port.HasValue)
                {
                    if ((scheme == "https" && port != 443) ||
                        (scheme == "http" && port != 80))
                    {
                        portString = $":{port}";
                    }
                }

                baseUrl = $"{scheme}://{host}{portString}/ticket/";
            }
            else if (!baseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }

            return baseUrl;
        }

        private byte[] GenerateQrCode(string text)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 150,
                    Width = 150,
                    Margin = 1
                }
            };

            var pixelData = writer.Write(text);

            using var image = Image.LoadPixelData<Rgba32>(
                pixelData.Pixels, pixelData.Width, pixelData.Height
            );

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }
    }

    public class IssueTicketsRequest
    {
        public int Count { get; set; }
        public Guid LotteryGroupId { get; set; }
    }

    public class CompleteTicketRequest
    {
        public bool Activate { get; set; }
    }
}
