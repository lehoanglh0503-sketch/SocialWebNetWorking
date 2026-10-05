using Microsoft.AspNetCore.SignalR;

namespace Social_Website.Hubs
{
    public class SocialHub : Hub
    {
        public async Task JoinUserGroup(long userId)
        {
            if (userId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
        }
    }
}
