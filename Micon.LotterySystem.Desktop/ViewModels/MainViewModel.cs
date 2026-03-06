using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micon.LotterySystem.Desktop.Models;
using Micon.LotterySystem.Desktop.Services;

namespace Micon.LotterySystem.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ITokenService _tokenService;
    private readonly IApiService _apiService;
    private bool _disposed;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private LotteryGroupInfo? _selectedLotteryGroup;

    public ObservableCollection<LotteryGroupInfo> LotteryGroups { get; } = [];

    public event Action? LogoutRequested;
    public event Action<LotteryGroupInfo>? NavigateToReceiptRequested;

    public MainViewModel(ITokenService tokenService, IApiService apiService)
    {
        _tokenService = tokenService;
        _apiService = apiService;
        _tokenService.OnTokenChanged += OnTokenChanged;
    }

    private void OnTokenChanged()
    {
        if (!_tokenService.IsAuthenticated)
        {
            LogoutRequested?.Invoke();
        }
    }

    public async Task RefreshOnNavigate()
    {
        await LoadLotteryGroupsAsync();
    }

    [RelayCommand]
    private void Logout()
    {
        _tokenService.ClearTokens();
        LogoutRequested?.Invoke();
    }

    [RelayCommand]
    private async Task RefreshLotteryGroups()
    {
        await LoadLotteryGroupsAsync();
    }

    private async Task LoadLotteryGroupsAsync()
    {
        IsLoading = true;
        StatusMessage = "抽選会一覧を取得中...";

        try
        {
            var groups = await _apiService.GetLotteryGroupsAsync();

            LotteryGroups.Clear();
            foreach (var group in groups)
            {
                LotteryGroups.Add(group);
            }

            if (LotteryGroups.Count > 0)
            {
                SelectedLotteryGroup = LotteryGroups[0];
            }

            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"抽選会一覧の取得に失敗しました: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToReceipt()
    {
        if (SelectedLotteryGroup != null)
        {
            NavigateToReceiptRequested?.Invoke(SelectedLotteryGroup);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _tokenService.OnTokenChanged -= OnTokenChanged;
            _disposed = true;
        }
    }
}
