// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace API.Util;

public static class DateTimeExtensions
{
    public static TimeSpan GetRoundTimeSpan(int roundMinutes)
    {
        var currentTime = DateTime.UtcNow;
        return GetRoundTime(currentTime, roundMinutes) - currentTime;
    }

    public static DateTime GetRoundTime(DateTime currentTime, int roundMinutes)
    {
        var minutesToAdd = roundMinutes - currentTime.Minute % roundMinutes;
        var nextRoundTime = currentTime.AddMinutes(minutesToAdd).AddSeconds(-currentTime.Second);

        return nextRoundTime;
    }

    public static long GetCurrentTimestamp()
    {
        return DateTimeOffset.Now.ToUnixTimeSeconds();
    }
}
