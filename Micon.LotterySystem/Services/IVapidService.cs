using WebPush;

namespace Micon.LotterySystem.Services
{
    public interface IVapidService
    {
        Task<VapidKeys> GetOrCreateKeysAsync();
    }
}
