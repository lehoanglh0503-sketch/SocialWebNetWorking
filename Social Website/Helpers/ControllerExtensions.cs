using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Social_Website.Models;

namespace Social_Website.Helpers
{
    public static class ControllerExtensions
    {
        public static long? GetCurrentUserId(this Controller controller)
        {
            return controller.HttpContext.Session.GetCurrentUserId();
        }

        public static bool IsAdmin(this Controller controller)
        {
            return controller.HttpContext.Session.IsAdmin();
        }

        public static User? GetCurrentUser(this Controller controller, SocialDbContext context)
        {
            var userId = controller.GetCurrentUserId();
            return userId != null ? context.Users.Find(userId.Value) : null;
        }
    }
}
