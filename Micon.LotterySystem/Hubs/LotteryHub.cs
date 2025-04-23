using Microsoft.AspNetCore.SignalR;

namespace Micon.LotterySystem.Hubs
{
    public class LotteryHub:Hub
    {
        public async Task SetLotteryGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }
        public async Task RemoveLotteryGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }

    }
}
