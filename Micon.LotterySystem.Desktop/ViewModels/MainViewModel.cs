using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micon.LotterySystem.Desktop.Services;
using Micon.LotterySystem.Desktop.Views;

namespace Micon.LotterySystem.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ITokenService _tokenService;
    private readonly IApiService _apiService;

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _statusMessage = "ログイン済み";

    public event Action? LogoutRequested;
    public event Action<UserControl>? NavigateRequested;

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

    [RelayCommand]
    private void Logout()
    {
        _tokenService.ClearTokens();
        LogoutRequested?.Invoke();
    }

    [RelayCommand]
    private void NavigateToReceipt()
    {
        var receiptViewModel = new ReceiptViewModel(_apiService);
        NavigateRequested?.Invoke(new ReceiptView { DataContext = receiptViewModel });
    }
}
