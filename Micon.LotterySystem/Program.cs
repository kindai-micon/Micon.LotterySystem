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
            // ← 必要最低限でOK。appsettings.json や環境変数も読み込まれる。
            var builder = WebApplication.CreateBuilder(args);

            // JSONループ防止など
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            // サービス登録
            builder.Services.AddScoped<IPasscodeService, PasscodeService>();
            builder.Services.AddSingleton<IAuthorityScanService, AuthorityScanService>();
            builder.Services.AddScoped<IAuthorizationHandler, DynamicRoleHandler>();

            // DB接続
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("lottery-db"));
            });

            // 認証・認可
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

            builder.Services.AddAuthorization(options =>
            {
                var authorityScanService = new AuthorityScanService();
                foreach (var auth in authorityScanService.Authority)
                {
                    options.AddPolicy(auth, policy =>
                        policy.Requirements.Add(new DynamicRoleRequirement(auth)));
                }
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // 開発時のみ Swagger
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

            // SPAルーティング（/api, /account 以外は index.html）
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

            // マイグレーション自動適用（開発環境限定にしたいなら if 文追加）
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            app.Run();
        }
    }
}
