using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Micon.LotterySystem.Desktop.Services;
using Micon.LotterySystem.Desktop.ViewModels;
using Micon.LotterySystem.Desktop.Views;
using System.Globalization;
using Micon.LotterySystem.Desktop.Settings;

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
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
            var apiService = _serviceProvider.GetRequiredService<IApiService>();

            var mainWindow = new MainWindow
            {
                DataContext = navigationService
            };

            // ログアウト時の処理
            if (navigationService is NavigationService ns)
            {
                ns.LogoutRequested += () =>
                {
                    navigationService.NavigateToLogin();
                };

                // 失敗チケットダイアログ表示
                ns.ShowFailedTicketsDialog += (failedTickets) =>
                {
                    ShowFailedTicketsDialog(mainWindow, failedTickets);
                };
            }

            // 起動時に認証状態を確認
            _ = InitializeAuthAsync(navigationService, tokenService, apiService);

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeAuthAsync(INavigationService navigationService, ITokenService tokenService, IApiService apiService)
    {
        try
        {
            // BaseUrlまたはリフレッシュトークンが存在しない場合はログイン画面
            if (string.IsNullOrEmpty(tokenService.BaseUrl) || string.IsNullOrEmpty(tokenService.RefreshToken))
            {
                navigationService.NavigateToLogin();
                return;
            }

            // リフレッシュトークンが期限切れの場合はログイン画面
            if (tokenService.IsRefreshTokenExpired)
            {
                tokenService.ClearTokens();
                navigationService.NavigateToLogin();
                return;
            }

            // BaseUrlを設定
            apiService.SetBaseUrl(tokenService.BaseUrl);

            // 起動時は常にアクセストークンを取得（サーバー側でトークン無効化を検出するため）
            var refreshResult = await apiService.RefreshTokenAsync(tokenService.RefreshToken!);

            if (refreshResult?.Error != null || string.IsNullOrEmpty(refreshResult?.AccessToken))
            {
                // リフレッシュ失敗時はトークンをクリアしてログイン画面へ
                tokenService.ClearTokens();
                navigationService.NavigateToLogin();
                return;
            }

            tokenService.SetTokens(
                refreshResult.AccessToken,
                refreshResult.RefreshToken,
                refreshResult.ExpiresIn,
                refreshResult.RefreshTokenExpiresIn,
                tokenService.BaseUrl
            );

            navigationService.NavigateToMain();

            // メイン画面のデータを読み込む
            if (navigationService is NavigationService ns)
            {
                await ns.RefreshCurrentMainViewModel();
            }
        }
        catch (Exception)
        {
            // エラー発生時はログイン画面へ
            tokenService.ClearTokens();
            navigationService.NavigateToLogin();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddSingleton(_configuration!);
        services.AddSingleton(BuildPrinterSettings(_configuration!));
        services.AddSingleton(BuildReceiptLayoutSettings(_configuration!));

        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddSingleton<ILocalStorageService, LocalStorageService>();
        services.AddSingleton<IWinRawPrinter, WinRawPrinter>();
        services.AddSingleton<ITicketRenderService, TicketRenderService>();
        services.AddSingleton<IReceiptPrinterService, ReceiptPrinterService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();

        // NavigationService（Factory関数を使用）
        services.AddSingleton<INavigationService>(sp => new NavigationService(
            sp.GetRequiredService<IApiService>(),
            sp.GetRequiredService<ILocalStorageService>(),
            sp.GetRequiredService<ITokenService>(),
            () => sp.GetRequiredService<LoginViewModel>(),
            () => sp.GetRequiredService<MainViewModel>()
        ));
    }

    private void ShowFailedTicketsDialog(Window parent, List<FailedTicketInfo> failedTickets)
    {
        var dialog = new MessageDialog(
            failedTickets.Select(t => (t.Number, t.Reason)).ToList()
        );
        dialog.ShowDialog(parent);
    }

    private static PrinterSettings BuildPrinterSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection("Printer");

        return new PrinterSettings
        {
            PrinterName = section["PrinterName"] ?? "POS-80C",
            DocumentName = section["DocumentName"] ?? "抽選券印刷",
            CutEnabled = ParseBool(section["CutEnabled"], true)
        };
    }

    private static ReceiptLayoutSettings BuildReceiptLayoutSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection("ReceiptLayout");
        var warningSection = section.GetSection("WarningLines");

        var warningLines = warningSection
            .GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();

        return new ReceiptLayoutSettings
        {
            PaperWidthPx = ParseInt(section["PaperWidthPx"], 576),
            QrSizePx = ParseInt(section["QrSizePx"], 220),
            MarginLeft = ParseInt(section["MarginLeft"], 24),
            MarginRight = ParseInt(section["MarginRight"], 24),
            MarginTop = ParseInt(section["MarginTop"], 24),
            MarginBottom = ParseInt(section["MarginBottom"], 24),
            TitleFontSize = ParseFloat(section["TitleFontSize"], 28),
            NumberFontSize = ParseFloat(section["NumberFontSize"], 42),
            BodyFontSize = ParseFloat(section["BodyFontSize"], 22),
            FooterFontSize = ParseFloat(section["FooterFontSize"], 18),
            Threshold = ParseInt(section["Threshold"], 160),
            WarningLines = warningLines.Count > 0
                ? warningLines
                : new List<string>
                {
                "本券は大切に保管してください",
                "抽選時までお持ちください"
                },
            FooterText = section["FooterText"] ?? "Micon Lottery System"
        };
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static float ParseFloat(string? value, float defaultValue)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }
}
