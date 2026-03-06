using WebPush;

namespace Micon.LotterySystem.Services
{
    public class VapidService : IVapidService
    {
        private const string VapidKeysFilePath = "vapid_keys.json";
        private static readonly SemaphoreSlim _lock = new(1, 1);
        public async Task<VapidKeys> GetOrCreateKeysAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (File.Exists(VapidKeysFilePath))
                {
                    var json = await File.ReadAllTextAsync(VapidKeysFilePath);
                    var keys = System.Text.Json.JsonSerializer.Deserialize<VapidKeys>(json);

                    if (keys != null && keys.PublicKey != null && keys.PrivateKey != null)
                    {
                        return new VapidKeys
                        {
                            PublicKey = keys.PublicKey,
                            PrivateKey = keys.PrivateKey
                        };
                    }
                }

                var newKeys = GenerateKeys();
                var newJson = System.Text.Json.JsonSerializer.Serialize(newKeys);
                await File.WriteAllTextAsync(VapidKeysFilePath, newJson);

                return newKeys;
            }
            finally
            {
                _lock.Release();
            }
        }

        private static VapidKeys GenerateKeys()
        {
            var vapidKeys = VapidHelper.GenerateVapidKeys();
            return new VapidKeys
            {
                PublicKey = vapidKeys.PublicKey,
                PrivateKey = vapidKeys.PrivateKey
            };
        }
    }

    public class VapidKeys
    {
        public string PublicKey { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
    }
}
