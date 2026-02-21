using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micon.LotterySystem.Desktop.Services;

namespace Micon.LotterySystem.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ITokenService _tokenService;

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _statusMessage = "ログイン済み";

    public event Action? LogoutRequested;

    public MainViewModel(ITokenService tokenService)
    {
        _tokenService = tokenService;
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
}
