
using Micon.LotterySystem.Handler;
using Micon.LotterySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Micon.LotterySystem.Services;
using System.Text.Json.Serialization;
using Micon.LotterySystem.Hubs;
using Microsoft.AspNetCore.HttpOverrides;
namespace Micon.LotterySystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
            builder.Services.AddScoped<IPasscodeService, PasscodeService>();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSingleton<IAuthorityScanService, AuthorityScanService>();
            builder.Services.AddSwaggerGen();
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
                option.DefaultScheme = IdentityConstants.ApplicationScheme;
                option.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            }).AddIdentityCookies();

            builder.Services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Stores.MaxLengthForKeys = 128;
                o.User.RequireUniqueEmail = false;
            })
                .AddDefaultTokenProviders()
                .AddRoles<ApplicationRole>()
                .AddUserManager<UserManager<ApplicationUser>>()
                .AddSignInManager<SignInManager<ApplicationUser>>()
                .AddErrorDescriber<JapaneseIdentityErrorDescriber>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            
            var app = builder.Build();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            // Configure the HTTP request pipeline.
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
                // /api で始まるリクエストはそのまま処理を続行
                if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)&& !context.Request.Path.StartsWithSegments("/account", StringComparison.OrdinalIgnoreCase))
                {
                    // index.html の内容を読み込む
                    var indexPath = Path.Combine(app.Environment.WebRootPath, "index.html");
                    if (File.Exists(indexPath))
                    {
                        context.Response.ContentType = "text/html";
                        await context.Response.SendFileAsync(indexPath);
                        return; // index.html を返したら処理を終了
                    }
                }

                await next(); // /api の場合は次のミドルウェアへ
            });
            using (var sp = app.Services.CreateScope())
            {
                var dbContext = sp.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
                var authorityScanService = sp.ServiceProvider.GetRequiredService<IAuthorityScanService>();
                
                var role =  dbContext.Roles.Where(x => x.Name == "Admin")
                    .Include(x => x.Authorities).FirstOrDefault();
                if(role != null)
                {
                    var nothave = authorityScanService.Authority.Where(x => !role.Authorities.Any(y => y.Name == x));
                    foreach(var auth in nothave)
                    {
                        Authority authority = new Authority()
                        {
                            Name = auth,
                            RoleId = role.Id,
                            Role = role
                        };
                        role.Authorities.Add(authority);
                        dbContext.Add(authority);
                        dbContext.SaveChanges();
                    }
                }
            }
            app.Run();
        }
    }
}
