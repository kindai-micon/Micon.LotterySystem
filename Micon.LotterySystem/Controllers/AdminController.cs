using Micon.LotterySystem.Models;
using Micon.LotterySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext applicationDbContext,
        SignInManager<ApplicationUser> signInManager,
        IPasscodeService passcodeService) : ControllerBase
    {
        /// <summary>
        /// すべてのデータを削除して、データベースを初期状態にリセットします。
        /// </summary>
        [Authorize(Policy = "DeleteAllData")]
        [HttpPost(nameof(DeleteAllData))]
        public async Task<IActionResult> DeleteAllData([FromBody] DeleteAllDataRequest request)
        {
            // 現在のユーザーを取得
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // パスワード検証
            var passwordValid = await userManager.CheckPasswordAsync(currentUser, request.Password);
            if (!passwordValid)
            {
                return BadRequest(new { error = "パスワードが正しくありません" });
            }

            try
            {
                // データベースを削除
                var deleted = await applicationDbContext.Database.EnsureDeletedAsync();

                // データベースを再作成してマイグレーションを実行
                await applicationDbContext.Database.MigrateAsync();

                // ログアウト
                await signInManager.SignOutAsync();

                return Ok(new { message = "すべてのデータが削除されました。データベースは初期状態にリセットされました。" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"エラーが発生しました: {ex.Message}" });
            }
        }

    }

    /// <summary>
    /// データ削除リクエスト
    /// </summary>
    public class DeleteAllDataRequest
    {
        /// <summary>
        /// 削除確認用パスワード
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

}
