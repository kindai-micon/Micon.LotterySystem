using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    Task<List<LotteryGroupInfo>> GetLotteryGroupsAsync();
    Task<IssueTicketsResponse> IssueTicketsAsync(int count, Guid lotteryGroupId);
    Task<byte[]?> GetQrCodeAsync(Guid displayId);
    Task<CompleteTicketResponse> CompleteTicketAsync(Guid displayId, bool activate);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ApiService> _logger;
    private string? _baseUrl;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ApiService(HttpClient httpClient, ITokenService tokenService, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _tokenService.OnTokenChanged += UpdateAuthHeader;
    }

    public void SetBaseUrl(string baseUrl)
    {
        // HttpClientのBaseAddressは一度リクエストを送信すると変更できないため、
        // 内部変数に保存して各リクエストでURLを構築する
        _baseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
    }

    private string BuildUrl(string endpoint)
    {
        if (string.IsNullOrEmpty(_baseUrl))
            throw new InvalidOperationException("BaseUrlが設定されていません");

        // endpointが先頭に/を持っている場合は削除
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        return $"{_baseUrl}{endpoint}";
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

        var response = await _httpClient.PostAsync(BuildUrl("api/desktop-auth/login"), content);
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<LoginResponse>(json, _jsonOptions)
            ?? new LoginResponse { Error = "レスポンスの解析に失敗しました" };
    }

    public async Task<RefreshResponse> RefreshTokenAsync(string refreshToken)
    {
        var data = new { refreshToken };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(BuildUrl("api/desktop-auth/refresh"), content);
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<RefreshResponse>(json, _jsonOptions)
            ?? new RefreshResponse { Error = "レスポンスの解析に失敗しました" };
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        await EnsureValidTokenAsync();

        try
        {
            var response = await _httpClient.GetAsync(BuildUrl(endpoint));

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }

            _logger.LogWarning("GET {Endpoint} failed with status {StatusCode}", endpoint, response.StatusCode);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET {Endpoint} threw exception", endpoint);
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

            var response = await _httpClient.PostAsync(BuildUrl(endpoint), content);
            var responseJson = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST {Endpoint} threw exception", endpoint);
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

            var response = await _httpClient.PostAsync(BuildUrl(endpoint), content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST {Endpoint} threw exception", endpoint);
            return false;
        }
    }

    private async Task EnsureValidTokenAsync()
    {
        if (!_tokenService.NeedsRefresh || _tokenService.IsRefreshTokenExpired)
            return;

        await _refreshLock.WaitAsync();
        try
        {
            // ロック取得後に再チェック（待機中に他が更新した可能性）
            if (!_tokenService.NeedsRefresh)
                return;

            var refreshResult = await RefreshTokenAsync(_tokenService.RefreshToken!);

            if (refreshResult != null && refreshResult.Error == null)
            {
                _tokenService.SetTokens(
                    refreshResult.AccessToken,
                    refreshResult.RefreshToken,
                    refreshResult.ExpiresIn,
                    refreshResult.RefreshTokenExpiresIn,
                    _tokenService.BaseUrl!
                );
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // Receipt API Methods

    public async Task<List<LotteryGroupInfo>> GetLotteryGroupsAsync()
    {
        await EnsureValidTokenAsync();

        if (string.IsNullOrEmpty(_baseUrl))
        {
            throw new InvalidOperationException("BaseUrlが設定されていません");
        }

        var response = await _httpClient.GetAsync(BuildUrl("api/receipt/lottery-groups"));

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"APIエラー: {response.StatusCode} - {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<LotteryGroupInfo>>(json, _jsonOptions);
        return result ?? [];
    }

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
            var response = await _httpClient.GetAsync(BuildUrl($"api/receipt/qrcode/{displayId}"));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            _logger.LogWarning("GetQrCode {DisplayId} failed with status {StatusCode}", displayId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetQrCode {DisplayId} threw exception", displayId);
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
