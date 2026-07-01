using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Micon.LotterySystem.Desktop.Models;

public class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refreshTokenExpiresIn")]
    public int RefreshTokenExpiresIn { get; set; }

    [JsonPropertyName("user")]
    public UserInfo? User { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = [];

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class UserInfo
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class RefreshResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refreshTokenExpiresIn")]
    public int RefreshTokenExpiresIn { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
