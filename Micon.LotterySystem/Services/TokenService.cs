using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Micon.LotterySystem.Models;

namespace Micon.LotterySystem.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public TokenService(
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _userManager = userManager;

            var jwtSettings = _configuration.GetSection("JwtSettings");
            _accessTokenExpirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");
            _refreshTokenExpirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "30");
        }

        public int AccessTokenExpirationSeconds => _accessTokenExpirationMinutes * 60;
        public int RefreshTokenExpirationSeconds => _refreshTokenExpirationDays * 24 * 60 * 60;

        public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(ApplicationUser user)
        {
            // マルチデバイス対応: 既存のトークンは無効化せず、新しいトークンを追加
            var refreshToken = new RefreshToken
            {
                Token = GenerateSecureToken(),
                UserId = user.Id,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(_refreshTokenExpirationDays),
                CreatedAt = DateTimeOffset.UtcNow,
                IsRevoked = false
            };

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<(string? AccessToken, RefreshToken? RefreshToken, string? Error)> RefreshAccessTokenAsync(string refreshToken)
        {
            var storedToken = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                return (null, null, "無効なリフレッシュトークンです");
            }

            if (storedToken.IsRevoked)
            {
                return (null, null, "リフレッシュトークンは無効化されています");
            }

            if (storedToken.IsExpired)
            {
                return (null, null, "リフレッシュトークンの有効期限が切れています");
            }

            // 古いリフレッシュトークンを無効化
            storedToken.IsRevoked = true;

            var user = storedToken.User;

            // ユーザーのロールを取得
            var roles = await _userManager.GetRolesAsync(user);

            // 新しいリフレッシュトークンを生成
            var newRefreshToken = await GenerateRefreshTokenAsync(user);

            // アクセストークンを生成（ロールあり）
            var accessToken = GenerateAccessToken(user, roles);

            return (accessToken, newRefreshToken, null);
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
