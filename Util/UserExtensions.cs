namespace API.Util;

public static class UserExtensions
{
    public static long SignId(ulong unsignedValue)
    {
        var bytes = BitConverter.GetBytes(unsignedValue);
        return BitConverter.ToInt64(bytes, 0);
    }

    public static ulong UnSignId(long signedValue)
    {
        var bytes = BitConverter.GetBytes(signedValue);
        return BitConverter.ToUInt64(bytes, 0);
    }
}