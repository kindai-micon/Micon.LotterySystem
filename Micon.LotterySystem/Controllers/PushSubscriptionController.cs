using Microsoft.AspNetCore.Mvc;
using Micon.LotterySystem.Models.API;
using Micon.LotterySystem.Models;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/push-subscription")]
    [ApiController]
    public class PushSubscriptionController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PushSubscriptionController(ApplicationDbContext applicationDbContext)
        {
            _db = applicationDbContext;
        }

        [HttpPost("{guid}")]
        public async Task<IActionResult> Subscribe(
            [FromRoute] Guid guid,
            [FromBody] PushSubscriptionDTO subscriptionDTO)
        {
            var subscription = await _db.PushSubscriptions.FindAsync(guid);
            if (subscription == null)
            {
                subscription = new PushSubscription
                {
                    DisplayId = guid,
                    Endpoint = subscriptionDTO.Endpoint,
                    P256dh = subscriptionDTO.Keys.P256dh,
                    Auth = subscriptionDTO.Keys.Auth
                };
                _db.PushSubscriptions.Add(subscription);
            }
            else
            {
                subscription.Endpoint = subscriptionDTO.Endpoint;
                subscription.P256dh = subscriptionDTO.Keys.P256dh;
                subscription.Auth = subscriptionDTO.Keys.Auth;
            }
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
