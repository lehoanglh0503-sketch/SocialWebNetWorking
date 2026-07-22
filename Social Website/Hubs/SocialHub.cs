using Microsoft.AspNetCore.SignalR;

namespace Social_Website.Hubs
{
    public class SocialHub : Hub
    {
        // Hub này được sử dụng để phát (broadcast) bài đăng mới, bình luận mới qua IHubContext ở Controller
    }
}
