using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Micon.LotterySystem.Desktop.Services;
using Micon.LotterySystem.Desktop.ViewModels;
using Micon.LotterySystem.Desktop.Views;

namespace Micon.LotterySystem.Desktop;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // ログイン状態に応じて初期画面を設定
            if (tokenService.IsAuthenticated)
            {
                mainViewModel.CurrentView = new MainView { DataContext = mainViewModel };
            }
            else
            {
                mainViewModel.CurrentView = new LoginView { DataContext = loginViewModel };
            }

            // ログイン成功時の処理
            loginViewModel.LoginSucceeded += () =>
            {
                mainViewModel.CurrentView = new MainView { DataContext = mainViewModel };
            };

            // ログアウト時の処理
            mainViewModel.LogoutRequested += () =>
            {
                var newLoginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
                newLoginViewModel.LoginSucceeded += () =>
                {
                    mainViewModel.CurrentView = new MainView { DataContext = mainViewModel };
                };
                mainViewModel.CurrentView = new LoginView { DataContext = newLoginViewModel };
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
    }
}
