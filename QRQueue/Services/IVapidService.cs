using WebPush;

namespace QRQueue.Services
{
    public interface IVapidService
    {
        Task<VapidKeys> GetOrCreateKeysAsync();
    }
}
