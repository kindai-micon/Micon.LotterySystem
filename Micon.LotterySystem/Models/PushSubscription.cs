namespace Micon.LotterySystem.Models
{
    public class PushSubscription:BaseModel
    {
        public Guid DisplayId { get; set; }
        public string Endpoint { get; set; }
        public string P256dh { get; set; }
        public string Auth { get; set; }
        public long? ExpirationTime { get; set; }
    }
}
