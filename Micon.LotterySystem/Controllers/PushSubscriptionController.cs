using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Micon.LotterySystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/push-subscription")]
    [ApiController]
    public class PushSubscriptionController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IVapidService _service;

        public PushSubscriptionController(ApplicationDbContext applicationDbContext,IVapidService vapidService)
        {
            _db = applicationDbContext;
            _service = vapidService;
        }

        [HttpPost("{guid}")]
        public async Task<IActionResult> Subscribe(
            [FromRoute] Guid guid,
            [FromBody] PushSubscriptionDTO subscriptionDTO)
        {
            var subscription = new PushSubscription
            {
                DisplayId = guid,
                Endpoint = subscriptionDTO.Endpoint,
                P256dh = subscriptionDTO.Keys.P256dh,
                Auth = subscriptionDTO.Keys.Auth
            };
            _db.PushSubscriptions.Add(subscription);

            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("vapid-public-key")]
        public async Task<IActionResult> GetVapidPublicKey()
        {
            var keys = await _service.GetOrCreateKeysAsync();
            if (keys.PublicKey != null && keys.PrivateKey != null)
            {
                return Ok(new { publicKey = keys.PublicKey});
            }
            return StatusCode(500, new { error = "Push notifications not configured" });
        }
    }
}
