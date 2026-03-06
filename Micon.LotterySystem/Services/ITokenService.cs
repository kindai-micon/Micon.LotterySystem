using Micon.LotterySystem.Models;

namespace Micon.LotterySystem.Services
{
    public interface ITokenService
    {
        /// <summary>
        /// アクセストークンを生成します
        /// </summary>
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);

        /// <summary>
        /// リフレッシュトークンを生成して保存します
        /// </summary>
        Task<RefreshToken> GenerateRefreshTokenAsync(ApplicationUser user);

        /// <summary>
        /// リフレッシュトークンを検証し、新しいアクセストークンとリフレッシュトークンを発行します
        /// 古いリフレッシュトークンは無効化され、新しいものに置き換わります
        /// </summary>
        Task<(string? AccessToken, RefreshToken? RefreshToken, string? Error)> RefreshAccessTokenAsync(string refreshToken);

        /// <summary>
        /// アクセストークンの有効期限（秒）
        /// </summary>
        int AccessTokenExpirationSeconds { get; }

        /// <summary>
        /// リフレッシュトークンの有効期限（秒）
        /// </summary>
        int RefreshTokenExpirationSeconds { get; }
    }
}
