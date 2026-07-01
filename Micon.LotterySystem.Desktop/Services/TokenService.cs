using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Micon.LotterySystem.Desktop.Services;

public interface ITokenService
{
    string? AccessToken { get; }
    string? RefreshToken { get; }
    string? BaseUrl { get; }
    DateTimeOffset? AccessTokenExpiresAt { get; }
    DateTimeOffset? RefreshTokenExpiresAt { get; }
    bool IsAuthenticated { get; }
    bool NeedsRefresh { get; }
    bool IsRefreshTokenExpired { get; }

    void SetTokens(string accessToken, string refreshToken, int expiresIn, int refreshTokenExpiresIn, string baseUrl);
    void ClearTokens();
    event Action? OnTokenChanged;
}

public class TokenService : ITokenService
{
    private readonly string _tokenFilePath;

    private string? _accessToken;
    private string? _refreshToken;
    private string? _baseUrl;
    private DateTimeOffset? _accessTokenExpiresAt;
    private DateTimeOffset? _refreshTokenExpiresAt;

    public string? AccessToken => _accessToken;
    public string? RefreshToken => _refreshToken;
    public string? BaseUrl => _baseUrl;
    public DateTimeOffset? AccessTokenExpiresAt => _accessTokenExpiresAt;
    public DateTimeOffset? RefreshTokenExpiresAt => _refreshTokenExpiresAt;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && !IsAccessTokenExpired;
    public bool NeedsRefresh => _accessTokenExpiresAt.HasValue && DateTimeOffset.UtcNow.AddMinutes(5) >= _accessTokenExpiresAt.Value;
    public bool IsRefreshTokenExpired => !_refreshTokenExpiresAt.HasValue || DateTimeOffset.UtcNow >= _refreshTokenExpiresAt.Value;

    private bool IsAccessTokenExpired => !_accessTokenExpiresAt.HasValue || DateTimeOffset.UtcNow >= _accessTokenExpiresAt.Value;

    public event Action? OnTokenChanged;

    public TokenService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "Micon.Lottery");
        Directory.CreateDirectory(appFolder); // フォルダが存在しない場合は作成
        _tokenFilePath = Path.Combine(appFolder, "tokens.dat");

        LoadTokens();
    }

    public void SetTokens(string accessToken, string refreshToken, int expiresIn, int refreshTokenExpiresIn, string baseUrl)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _baseUrl = baseUrl;
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        _refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(refreshTokenExpiresIn);

        SaveTokens();
        OnTokenChanged?.Invoke();
    }

    public void ClearTokens()
    {
        _accessToken = null;
        _refreshToken = null;
        _baseUrl = null;
        _accessTokenExpiresAt = null;
        _refreshTokenExpiresAt = null;

        DeleteTokenFile();
        OnTokenChanged?.Invoke();
    }

    private void SaveTokens()
    {
        try
        {
            var data = new TokenData
            {
                AccessToken = _accessToken,
                RefreshToken = _refreshToken,
                BaseUrl = _baseUrl,
                AccessTokenExpiresAt = _accessTokenExpiresAt?.UtcDateTime,
                RefreshTokenExpiresAt = _refreshTokenExpiresAt?.UtcDateTime
            };

            var json = JsonSerializer.Serialize(data);
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            // DPAPIで暗号化（現在のユーザーのみ復号可能）
            var encryptedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_tokenFilePath, encryptedBytes);
        }
        catch
        {
            // 暗号化/保存に失敗した場合は静かに失敗
            // メモリ上にはトークンが残るので、再ログインまでは動作する
        }
    }

    private void LoadTokens()
    {
        if (!File.Exists(_tokenFilePath))
            return;

        try
        {
            var encryptedBytes = File.ReadAllBytes(_tokenFilePath);

            // DPAPIで復号化
            var jsonBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            var data = JsonSerializer.Deserialize<TokenData>(json);

            if (data != null)
            {
                _accessToken = data.AccessToken;
                _refreshToken = data.RefreshToken;
                _baseUrl = data.BaseUrl;
                _accessTokenExpiresAt = data.AccessTokenExpiresAt.HasValue
                    ? new DateTimeOffset(data.AccessTokenExpiresAt.Value, TimeSpan.Zero)
                    : null;
                _refreshTokenExpiresAt = data.RefreshTokenExpiresAt.HasValue
                    ? new DateTimeOffset(data.RefreshTokenExpiresAt.Value, TimeSpan.Zero)
                    : null;
            }
        }
        catch
        {
            // 復号化に失敗した（別ユーザー/マシンなど）場合はトークンをクリア
            ClearTokens();
        }
    }

    private void DeleteTokenFile()
    {
        try
        {
            if (File.Exists(_tokenFilePath))
                File.Delete(_tokenFilePath);
        }
        catch
        {
            // ファイル削除に失敗しても問題ない
        }
    }

    private class TokenData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? BaseUrl { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }
}
