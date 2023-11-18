using System.Collections.Concurrent;
using System.Text.Json;
using DotNetBungieAPI.Models.Authorization;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace API.Services;

public static class BungieAuthCacheService
{
    private static readonly
        ConcurrentDictionary<long, (OAuthCreatingTicketContext Context, AuthorizationTokenData Token)>
        AuthContexts = new();

    private static JsonSerializerOptions? _jsonSerializerOptions;

    public static void Initialize(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public static void TryAddContext(OAuthCreatingTicketContext authCreatingTicketContext)
    {
        var tokenData =
            authCreatingTicketContext.TokenResponse.Response!.Deserialize<AuthorizationTokenData>(
                _jsonSerializerOptions);
        AuthContexts.TryAdd(tokenData!.MembershipId, (authCreatingTicketContext, tokenData));
    }

    public static bool GetByIdAndRemove(long id,
        out (OAuthCreatingTicketContext Context, AuthorizationTokenData Token) context)
    {
        return AuthContexts.TryRemove(id, out context);
    }
}
