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
using Micon.LotterySystem.Hubs;
namespace Micon.LotterySystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // �� �K�v�Œ����OK�Bappsettings.json ����ϐ���ǂݍ��܂��B
            var builder = WebApplication.CreateBuilder(args);

            // JSON���[�v�h�~�Ȃ�
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            // �T�[�r�X�o�^
            builder.Services.AddScoped<IPasscodeService, PasscodeService>();
            builder.Services.AddSingleton<IAuthorityScanService, AuthorityScanService>();
            builder.Services.AddScoped<IAuthorizationHandler, DynamicRoleHandler>();
            builder.Services.AddSignalR();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("lottery-db"));
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()   // すべてのオリジンからのアクセスを許可
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });
            builder.Services.AddAuthorization(options =>
            {
                AuthorityScanService authorityScanService = new AuthorityScanService();
                foreach(var auth in authorityScanService.Authority)
                {
                    options.AddPolicy(auth, policy =>
                    policy.Requirements.Add(new DynamicRoleRequirement(auth)));
                }
                

            });
            builder.Services.AddAuthentication(option =>
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

            // �J�����̂� Swagger
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
            app.MapHub<LotteryHub>("/api/lotteryHub");
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

            // �}�C�O���[�V���������K�p�i�J��������ɂ������Ȃ� if ���ǉ��j
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            app.Run();
        }
    }
}
