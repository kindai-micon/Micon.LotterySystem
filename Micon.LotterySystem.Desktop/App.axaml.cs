using System;
using System.IO;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micon.LotterySystem.Desktop.Services;
using Micon.LotterySystem.Desktop.ViewModels;
using Micon.LotterySystem.Desktop.Views;

namespace Micon.LotterySystem.Desktop;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private IConfiguration? _configuration;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
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

            if (tokenService.IsAuthenticated)
            {
                mainViewModel.CurrentView = new MainView { DataContext = mainViewModel };
            }
            else
            {
                mainViewModel.CurrentView = new LoginView { DataContext = loginViewModel };
            }

            loginViewModel.LoginSucceeded += () =>
            {
                mainViewModel.CurrentView = new MainView { DataContext = mainViewModel };
            };

            mainViewModel.LogoutRequested += () =>
            {
                var newLoginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
                newLoginViewModel.LoginSucceeded += () =>
                {
                    mainViewModel.CurrentView = new MainView { DataContext = mainViewModel };
                };
                mainViewModel.CurrentView = new LoginView { DataContext = newLoginViewModel };
            };

            mainViewModel.NavigateRequested += (view) =>
            {
                mainViewModel.CurrentView = view;
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_configuration!);
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
    }
}
