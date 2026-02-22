using Microsoft.AspNetCore.Mvc;
using System;

namespace Micon.LotterySystem.Controllers
[ApiController]
[Route("api/push-subscription")]
{
	public PushSubscriptionController(ApplicationDbContext applicationDbContext) : ControllerBase
	{
		[HttpPost("{guid}")]
		public async Task<IActionResult> Subscribe([FromRoute] Guid guid,[FromBody] PushSubscriptionDTO subscription,)
		{
        //TODO : 保存された購読情報をデータベースに保存するロジックを実装する
    }
}
}
