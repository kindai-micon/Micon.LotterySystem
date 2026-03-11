using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketInfoController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TicketInfoController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 指定した抽選会のチケット情報を取得
        /// </summary>
        [HttpGet("{lotteryGroupId}")]
        public async Task<IActionResult> GetTicketInfo(Guid lotteryGroupId)
        {
            var lotteryGroup = await _db.LotteryGroups
                .Include(g => g.TicketInfo)
                .FirstOrDefaultAsync(g => g.DisplayId == lotteryGroupId);

            if (lotteryGroup == null)
            {
                return NotFound("抽選会が見つかりません");
            }

            if (lotteryGroup.TicketInfo == null)
            {
                return NotFound("チケット情報が見つかりません");
            }

            var response = new TicketInfoResponse
            {
                Name = lotteryGroup.Name,
                TicketLabel = lotteryGroup.TicketInfo.TicketLabel,
                Description = lotteryGroup.TicketInfo.Description,
                Warning = lotteryGroup.TicketInfo.Warning,
                WarningText = lotteryGroup.TicketInfo.WarningText,
                FooterText = lotteryGroup.TicketInfo.FooterText
            };

            return Ok(response);
        }

        /// <summary>
        /// 指定した抽選会のチケット情報を更新
        /// </summary>
        [Authorize(Policy = "TicketPublish")]
        [HttpPut("{lotteryGroupId}")]
        public async Task<IActionResult> UpdateTicketInfo(Guid lotteryGroupId, [FromBody] TicketInfoUpdateRequest request)
        {
            var lotteryGroup = await _db.LotteryGroups
                .Include(g => g.TicketInfo)
                .FirstOrDefaultAsync(g => g.DisplayId == lotteryGroupId);

            if (lotteryGroup == null)
            {
                return NotFound("抽選会が見つかりません");
            }

            if (lotteryGroup.TicketInfo == null)
            {
                return NotFound("チケット情報が見つかりません");
            }

            // 更新
            lotteryGroup.TicketInfo.TicketLabel = request.TicketLabel ?? "抽選券";
            lotteryGroup.TicketInfo.Description = request.Description ?? "";
            lotteryGroup.TicketInfo.WarningText = request.WarningText ?? "";
            lotteryGroup.TicketInfo.FooterText = request.FooterText;

            await _db.SaveChangesAsync();

            var response = new TicketInfoResponse
            {
                Name = lotteryGroup.Name,
                TicketLabel = lotteryGroup.TicketInfo.TicketLabel,
                Description = lotteryGroup.TicketInfo.Description,
                Warning = lotteryGroup.TicketInfo.Warning,
                WarningText = lotteryGroup.TicketInfo.WarningText,
                FooterText = lotteryGroup.TicketInfo.FooterText
            };

            return Ok(response);
        }
    }

    /// <summary>
    /// TicketInfo更新リクエスト用DTO
    /// </summary>
    public class TicketInfoUpdateRequest
    {
        public string? TicketLabel { get; set; }
        public string? Description { get; set; }
        public string? WarningText { get; set; }
        public string? FooterText { get; set; }
    }
}
