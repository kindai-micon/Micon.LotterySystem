using QRQueue.Models;

namespace QRQueue.Services
{
    public interface IPushSubscriptionService
    {
        Task SendLotteryPushAsync(Ticket ticket);
    }
}
