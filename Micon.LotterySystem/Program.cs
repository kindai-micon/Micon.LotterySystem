using Micon.LotterySystem.Handler;
using Micon.LotterySystem.Models;
using Micon.LotterySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace Micon.LotterySystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ✅ 明示的に appsettings.json を読み込むようにする
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = Directory.GetCurrentDirectory(),
                EnvironmentName = Environments.Development // ← appsettings.Development.json も対象になる
            });

            // ✅ デバッグ出力（削除してOK）
            Console.WriteLine("接続文字列: " + builder.Configuration.GetConnectionString("lottery-db"));

            // サービスの登録
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            builder.Services.AddScoped<IPasscodeService, PasscodeService>();
            builder.Services.AddSingleton<IAuthorityScanService, AuthorityScanService>();
            builder.Services.AddScoped<IAuthorizationHandler, DynamicRoleHandler>();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("lottery-db"));
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            }).AddIdentityCookies();

            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Stores.MaxLengthForKeys = 128;
                options.User.RequireUniqueEmail = false;
            })
            .AddDefaultTokenProviders()
            .AddRoles<ApplicationRole>()
            .AddUserManager<UserManager<ApplicationUser>>()
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddErrorDescriber<JapaneseIdentityErrorDescriber>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // 認可ポリシーの追加
            builder.Services.AddAuthorization(options =>
            {
                AuthorityScanService authorityScanService = new AuthorityScanService();
                foreach (var auth in authorityScanService.Authority)
                {
                    options.AddPolicy(auth, policy =>
                        policy.Requirements.Add(new DynamicRoleRequirement(auth)));
                }
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // SPA対応：index.html にリダイレクト（/api, /account 以外）
            app.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
                    && !context.Request.Path.StartsWithSegments("/account", StringComparison.OrdinalIgnoreCase))
                {
                    var indexPath = Path.Combine(app.Environment.WebRootPath, "index.html");
                    if (File.Exists(indexPath))
                    {
                        context.Response.ContentType = "text/html";
                        await context.Response.SendFileAsync(indexPath);
                        return;
                    }
                }

                await next();
            });

            // マイグレーション実行（開発時のみ）
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            app.Run();
        }
    }
}
