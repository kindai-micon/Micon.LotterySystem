using Micon.LotterySystem.Handler;
using Micon.LotterySystem.Models;
using Micon.LotterySystem.Services;
using Micon.LotterySystem.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text.Json.Serialization;

namespace Micon.LotterySystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // JSONループ防止など
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });

            // DB接続
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("lottery-db")));

            // SignalR
            builder.Services.AddSignalR();

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // 認証 (Authentication)
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

            // Identityユーザー管理
            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Stores.MaxLengthForKeys = 128;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddUserManager<UserManager<ApplicationUser>>()
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddErrorDescriber<JapaneseIdentityErrorDescriber>();

            // サービス登録
            builder.Services.AddScoped<IPasscodeService, PasscodeService>();
            builder.Services.AddSingleton<IAuthorityScanService, AuthorityScanService>();
            builder.Services.AddScoped<IAuthorizationHandler, DynamicRoleHandler>();

            // 認可 (Authorization)
            builder.Services.AddAuthorization(options =>
            {
                var scanner = new AuthorityScanService();
                foreach (var auth in scanner.Authority)
                {
                    options.AddPolicy(auth, policy =>
                        policy.Requirements.Add(new DynamicRoleRequirement(auth)));
                }
            });

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // 開発時のみ Swagger UI を有効化
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");
            app.UseWebSockets();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<LotteryHub>("/api/lotteryHub");  // ダブルクォートに修正

            // SPA fallback
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

            // マイグレーション自動適用（開発環境などで）
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            app.Run();
        }
    }
}