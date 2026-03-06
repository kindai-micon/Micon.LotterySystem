using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Micon.LotterySystem.Desktop.Models;
using Micon.LotterySystem.Desktop.ViewModels;

namespace Micon.LotterySystem.Desktop.Services;

public class NavigationService : INavigationService, INotifyPropertyChanged
{
    private readonly IApiService _apiService;
    private readonly ILocalStorageService _localStorage;
    private readonly ITokenService _tokenService;
    private readonly Func<LoginViewModel> _loginViewModelFactory;
    private readonly Func<MainViewModel> _mainViewModelFactory;

    private ViewModelBase? _currentViewModel;
    private MainViewModel? _mainViewModel;
    private LoginViewModel? _currentLoginViewModel;
    private ReceiptViewModel? _currentReceiptViewModel;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (_currentViewModel != value)
            {
                _currentViewModel = value;
                OnPropertyChanged();
                CurrentViewModelChanged?.Invoke();
            }
        }
    }

    public event Action? CurrentViewModelChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? LogoutRequested;
    public event Action<List<FailedTicketInfo>>? ShowFailedTicketsDialog;

    public NavigationService(
        IApiService apiService,
        ILocalStorageService localStorage,
        ITokenService tokenService,
        Func<LoginViewModel> loginViewModelFactory,
        Func<MainViewModel> mainViewModelFactory)
    {
        _apiService = apiService;
        _localStorage = localStorage;
        _tokenService = tokenService;
        _loginViewModelFactory = loginViewModelFactory;
        _mainViewModelFactory = mainViewModelFactory;
    }

    public void NavigateToLogin()
    {
        // 古いLoginViewModelの購読を解除
        if (_currentLoginViewModel != null)
        {
            _currentLoginViewModel.LoginSucceeded -= OnLoginSucceeded;
        }

        _currentLoginViewModel = _loginViewModelFactory();
        _currentLoginViewModel.LoginSucceeded += OnLoginSucceeded;
        CurrentViewModel = _currentLoginViewModel;
    }

    public void NavigateToMain()
    {
        if (_mainViewModel == null)
        {
            _mainViewModel = _mainViewModelFactory();
            _mainViewModel.NavigateToReceiptRequested += OnNavigateToReceiptRequested;
            _mainViewModel.LogoutRequested += OnLogoutRequested;
        }
        CurrentViewModel = _mainViewModel;
    }

    public void NavigateToReceipt(LotteryGroupInfo lotteryGroup)
    {
        // 古いReceiptViewModelの購読を解除
        if (_currentReceiptViewModel != null)
        {
            _currentReceiptViewModel.BackRequested -= OnBackRequested;
            _currentReceiptViewModel.ShowFailedTicketsDialog -= OnShowFailedTicketsDialog;
        }

        _currentReceiptViewModel = new ReceiptViewModel(_apiService, _localStorage, lotteryGroup);
        _currentReceiptViewModel.BackRequested += OnBackRequested;
        _currentReceiptViewModel.ShowFailedTicketsDialog += OnShowFailedTicketsDialog;
        CurrentViewModel = _currentReceiptViewModel;
    }

    public async Task RefreshCurrentMainViewModel()
    {
        if (_mainViewModel != null)
        {
            await _mainViewModel.RefreshOnNavigate();
        }
    }

    private void OnLoginSucceeded()
    {
        NavigateToMain();
        _mainViewModel?.RefreshOnNavigate();
    }

    private void OnNavigateToReceiptRequested(LotteryGroupInfo lotteryGroup)
    {
        NavigateToReceipt(lotteryGroup);
    }

    private void OnLogoutRequested()
    {
        LogoutRequested?.Invoke();
    }

    private void OnBackRequested()
    {
        NavigateToMain();
        _mainViewModel?.RefreshOnNavigate();
    }

    private void OnShowFailedTicketsDialog(List<FailedTicketInfo> failedTickets)
    {
        ShowFailedTicketsDialog?.Invoke(failedTickets);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
