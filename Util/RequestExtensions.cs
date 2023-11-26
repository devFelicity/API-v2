namespace API.Util;

public static class RequestExtensions
{
    public static bool IsAuthorized(this HttpContext context)
    {
        return context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
               authHeader.ToString().Equals("Bearer " + Variables.SecurityKey);
    }
}
