using System;
using System.IO;
using System.Text.Json;

namespace Micon.LotterySystem.Desktop.Services;

public interface ITokenService
{
    string? AccessToken { get; }
    string? RefreshToken { get; }
    DateTimeOffset? AccessTokenExpiresAt { get; }
    DateTimeOffset? RefreshTokenExpiresAt { get; }
    bool IsAuthenticated { get; }
    bool NeedsRefresh { get; }
    bool IsRefreshTokenExpired { get; }

    void SetTokens(string accessToken, string refreshToken, int expiresIn, int refreshTokenExpiresIn);
    void ClearTokens();
    event Action? OnTokenChanged;
}

public class TokenService : ITokenService
{
    private const string TokenFilePath = "tokens.json";

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset? _accessTokenExpiresAt;
    private DateTimeOffset? _refreshTokenExpiresAt;

    public string? AccessToken => _accessToken;
    public string? RefreshToken => _refreshToken;
    public DateTimeOffset? AccessTokenExpiresAt => _accessTokenExpiresAt;
    public DateTimeOffset? RefreshTokenExpiresAt => _refreshTokenExpiresAt;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && !IsAccessTokenExpired;
    public bool NeedsRefresh => _accessTokenExpiresAt.HasValue && DateTimeOffset.UtcNow.AddMinutes(5) >= _accessTokenExpiresAt.Value;
    public bool IsRefreshTokenExpired => !_refreshTokenExpiresAt.HasValue || DateTimeOffset.UtcNow >= _refreshTokenExpiresAt.Value;

    private bool IsAccessTokenExpired => !_accessTokenExpiresAt.HasValue || DateTimeOffset.UtcNow >= _accessTokenExpiresAt.Value;

    public event Action? OnTokenChanged;

    public TokenService()
    {
        LoadTokens();
    }

    public void SetTokens(string accessToken, string refreshToken, int expiresIn, int refreshTokenExpiresIn)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        _refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(refreshTokenExpiresIn);

        SaveTokens();
        OnTokenChanged?.Invoke();
    }

    public void ClearTokens()
    {
        _accessToken = null;
        _refreshToken = null;
        _accessTokenExpiresAt = null;
        _refreshTokenExpiresAt = null;

        DeleteTokenFile();
        OnTokenChanged?.Invoke();
    }

    private void SaveTokens()
    {
        var data = new TokenData
        {
            AccessToken = _accessToken,
            RefreshToken = _refreshToken,
            AccessTokenExpiresAt = _accessTokenExpiresAt?.UtcDateTime,
            RefreshTokenExpiresAt = _refreshTokenExpiresAt?.UtcDateTime
        };

        var json = JsonSerializer.Serialize(data);
        File.WriteAllText(TokenFilePath, json);
    }

    private void LoadTokens()
    {
        if (!File.Exists(TokenFilePath))
            return;

        try
        {
            var json = File.ReadAllText(TokenFilePath);
            var data = JsonSerializer.Deserialize<TokenData>(json);

            if (data != null)
            {
                _accessToken = data.AccessToken;
                _refreshToken = data.RefreshToken;
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
            ClearTokens();
        }
    }

    private void DeleteTokenFile()
    {
        if (File.Exists(TokenFilePath))
            File.Delete(TokenFilePath);
    }

    private class TokenData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }
}
