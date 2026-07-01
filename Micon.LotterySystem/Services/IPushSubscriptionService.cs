using Micon.LotterySystem.Models;

namespace Micon.LotterySystem.Services
{
    public interface IPushSubscriptionService
    {
        Task SendLotteryPushAsync(Ticket ticket);
    }
}
