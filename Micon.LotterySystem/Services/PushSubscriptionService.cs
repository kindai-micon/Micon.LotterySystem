using Micon.LotterySystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using WebPush;

namespace Micon.LotterySystem.Services
{
    public class PushSubscriptionService(
        IConfiguration configuration,
        IVapidService vapidService,
        IServiceScopeFactory scopeFactory) : IPushSubscriptionService
    {
        public async Task SendLotteryPushAsync(Ticket ticket)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var vapidSection = configuration.GetSection("Vapid");
                var vapidKey = await vapidService.GetOrCreateKeysAsync();

                var vapidDetails = new VapidDetails(
                    vapidSection["Subject"],
                    vapidKey.PublicKey,
                    vapidKey.PrivateKey
                );

                var webPushClient = new WebPushClient();

                var subscriptions = await db.PushSubscriptions
                    .Where(s => s.DisplayId == ticket.DisplayId)
                    .ToListAsync();

                string payload = JsonSerializer.Serialize(new
                {
                    title = "通知",
                    body = ticket.Number + "番が当選しました!",
                    url = "ticket/" + ticket.DisplayId,
                    icon = "./favicon.png"
                });

                foreach (var subscription in subscriptions)
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );

                    try
                    {
                        await webPushClient.SendNotificationAsync(
                            pushSubscription,
                            payload,
                            vapidDetails
                        );
                    }
                    catch (WebPushException)
                    {
                        db.PushSubscriptions.Remove(subscription);
                    }
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Push batch error: {ex}");
            }
        }
    }
}