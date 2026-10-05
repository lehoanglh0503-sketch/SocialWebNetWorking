using System;

namespace Social_Website.Helpers
{
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

        private static TimeZoneInfo GetVietnamTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("GMT+7", TimeSpan.FromHours(7), "GMT+7", "GMT+7");
                }
            }
        }

        public static DateTime ToVietnamTime(this DateTime dateTime)
        {
            var utc = dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
        }

        public static string ToVietnamTimeString(this DateTime dateTime)
        {
            return dateTime.ToVietnamTime().ToString("dd/MM/yyyy HH:mm");
        }

        public static string ToVietnamTimeString(this DateTime dateTime, string format)
        {
            return dateTime.ToVietnamTime().ToString(format);
        }
    }
}
