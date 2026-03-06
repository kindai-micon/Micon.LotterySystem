namespace Micon.LotterySystem.Models.API
{
    public class PushSubscriptionDTO
    {
        public string Endpoint { get; set; }
        public long? ExpirationTime { get; set; }
        public PushSubscriptionKeysDTO Keys { get; set; }
    }

    public class PushSubscriptionKeysDTO
    {
        public string P256dh { get; set; }
        public string Auth { get; set; }
    }
}
