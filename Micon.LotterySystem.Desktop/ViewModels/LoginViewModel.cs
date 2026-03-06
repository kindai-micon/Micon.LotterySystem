using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Micon.LotterySystem.Desktop.Services;

namespace Micon.LotterySystem.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IApiService _apiService;
    private readonly ITokenService _tokenService;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _baseUrl = "http://localhost:5000/";

    public event Action? LoginSucceeded;

    public LoginViewModel(IApiService apiService, ITokenService tokenService, IConfiguration configuration)
    {
        _apiService = apiService;
        _tokenService = tokenService;

        // 優先順位: 環境変数 > appsettings.json > デフォルト値
        var backendUrl = configuration["BACKEND_URL"]
            ?? configuration["Backend:Url"];

        if (!string.IsNullOrEmpty(backendUrl))
        {
            _baseUrl = backendUrl.EndsWith("/") ? backendUrl : backendUrl + "/";
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "ユーザー名とパスワードを入力してください";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            _apiService.SetBaseUrl(BaseUrl.TrimEnd('/'));

            var result = await _apiService.LoginAsync(UserName, Password);

            if (!string.IsNullOrEmpty(result.Error))
            {
                ErrorMessage = result.Error;
            }
            else
            {
                _tokenService.SetTokens(
                    result.AccessToken,
                    result.RefreshToken,
                    result.ExpiresIn,
                    result.RefreshTokenExpiresIn,
                    BaseUrl.TrimEnd('/')
                );

                LoginSucceeded?.Invoke();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ログインエラー: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
