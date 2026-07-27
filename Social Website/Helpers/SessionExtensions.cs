using Microsoft.AspNetCore.Http;

namespace Social_Website.Helpers
{
    public static class SessionExtensions
    {
        public static long? GetCurrentUserId(this ISession session)
        {
            var userIdStr = session.GetString("UserId");
            return userIdStr != null ? long.Parse(userIdStr) : null;
        }

        public static bool IsAdmin(this ISession session)
        {
            return session.GetString("IsAdmin") == "True";
        }
    }
}
