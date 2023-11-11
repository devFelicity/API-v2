// ReSharper disable UnusedMember.Global

namespace API.Util;

public static class DateTimeExtensions
{
    public static DateTime GetRoundedDateTime(int roundMinutes)
    {
        var currentTime = DateTime.UtcNow;
        var minutesToAdd = roundMinutes - currentTime.Minute % roundMinutes;
        var nextRoundTime = currentTime.AddMinutes(minutesToAdd).AddSeconds(-currentTime.Second);
        return nextRoundTime;
    }

    public static TimeSpan GetRoundedTimeSpan(int roundMinutes)
    {
        var currentTime = DateTime.UtcNow;
        var minutesToAdd = roundMinutes - currentTime.Minute % roundMinutes;
        var nextRoundTime = currentTime.AddMinutes(minutesToAdd).AddSeconds(-currentTime.Second);
        return nextRoundTime - currentTime;
    }
}