using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Micon.LotterySystem.Services;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/desktop-auth")]
    [ApiController]
    public class DesktopAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public DesktopAuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginNameModel loginModel)
        {
            var user = await _userManager.FindByNameAsync(loginModel.UserName);
            if (user == null)
            {
                return Unauthorized(new { error = "ユーザー名またはパスワードが正しくありません" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginModel.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { error = "ユーザー名またはパスワードが正しくありません" });
            }

            // ロールを取得
            var roles = await _userManager.GetRolesAsync(user);

            // アクセストークンとリフレッシュトークンを生成
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            return Ok(new
            {
                accessToken,
                refreshToken = refreshToken.Token,
                expiresIn = _tokenService.AccessTokenExpirationSeconds,
                refreshTokenExpiresIn = (int)(refreshToken.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds,
                user = new
                {
                    id = user.Id,
                    userName = user.UserName,
                    email = user.Email
                },
                roles
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var (accessToken, refreshToken, error) = await _tokenService.RefreshAccessTokenAsync(request.RefreshToken);

            if (error != null)
            {
                return Unauthorized(new { error });
            }

            return Ok(new
            {
                accessToken,
                refreshToken = refreshToken!.Token,
                expiresIn = _tokenService.AccessTokenExpirationSeconds,
                refreshTokenExpiresIn = (int)(refreshToken.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds
            });
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
