using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Micon.LotterySystem.Desktop.Models;

namespace Micon.LotterySystem.Desktop.Services;

public interface IApiService
{
    void SetBaseUrl(string baseUrl);
    Task<LoginResponse> LoginAsync(string userName, string password);
    Task<RefreshResponse> RefreshTokenAsync(string refreshToken);
    Task<T?> GetAsync<T>(string endpoint);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
    Task<bool> PostAsync<TRequest>(string endpoint, TRequest data);

    // Receipt API
    Task<IssueTicketsResponse> IssueTicketsAsync(int count, Guid lotteryGroupId);
    Task<byte[]?> GetQrCodeAsync(Guid displayId);
    Task<CompleteTicketResponse> CompleteTicketAsync(Guid displayId, bool activate);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient, ITokenService tokenService)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _tokenService.OnTokenChanged += UpdateAuthHeader;
    }

    public void SetBaseUrl(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    private void UpdateAuthHeader()
    {
        if (!string.IsNullOrEmpty(_tokenService.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenService.AccessToken);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<LoginResponse> LoginAsync(string userName, string password)
    {
        var data = new { userName, password };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/desktop-auth/login", content);
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<LoginResponse>(json, _jsonOptions)
            ?? new LoginResponse { Error = "レスポンスの解析に失敗しました" };
    }

    public async Task<RefreshResponse> RefreshTokenAsync(string refreshToken)
    {
        var data = new { refreshToken };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/desktop-auth/refresh", content);
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<RefreshResponse>(json, _jsonOptions)
            ?? new RefreshResponse { Error = "レスポンスの解析に失敗しました" };
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        await EnsureValidTokenAsync();

        try
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }

            return default;
        }
        catch
        {
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        await EnsureValidTokenAsync();

        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
    {
        await EnsureValidTokenAsync();

        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureValidTokenAsync()
    {
        if (_tokenService.NeedsRefresh && !_tokenService.IsRefreshTokenExpired)
        {
            var refreshResult = await RefreshTokenAsync(_tokenService.RefreshToken!);

            if (refreshResult != null && refreshResult.Error == null)
            {
                _tokenService.SetTokens(
                    refreshResult.AccessToken,
                    refreshResult.RefreshToken,
                    refreshResult.ExpiresIn,
                    refreshResult.RefreshTokenExpiresIn
                );
            }
        }
    }

    // Receipt API Methods

    public async Task<IssueTicketsResponse> IssueTicketsAsync(int count, Guid lotteryGroupId)
    {
        var result = await PostAsync<IssueTicketsRequest, IssueTicketsResponse>(
            "api/receipt/tickets",
            new IssueTicketsRequest { Count = count, LotteryGroupId = lotteryGroupId }
        );

        return result ?? new IssueTicketsResponse { Error = "チケット発行に失敗しました" };
    }

    public async Task<byte[]?> GetQrCodeAsync(Guid displayId)
    {
        await EnsureValidTokenAsync();

        try
        {
            var response = await _httpClient.GetAsync($"api/receipt/qrcode/{displayId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CompleteTicketResponse> CompleteTicketAsync(Guid displayId, bool activate)
    {
        var result = await PostAsync<CompleteTicketRequest, CompleteTicketResponse>(
            $"api/receipt/complete/{displayId}",
            new CompleteTicketRequest { Activate = activate }
        );

        return result ?? new CompleteTicketResponse { Success = false, Error = "通信エラー" };
    }
}
