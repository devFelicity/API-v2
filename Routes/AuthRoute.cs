using System.Security.Claims;
using API.Contexts;
using API.Contexts.Objects;
using API.Services;
using API.Util;
using DotNetBungieAPI.AspNet.Security.OAuth.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace API.Routes;

public static class AuthRoute
{
    private static readonly TimedDictionary<ulong, string> AuthCache = new(TimeSpan.FromMinutes(2));

    public static void MapAuth(this RouteGroupBuilder group)
    {
        group.MapGet("/bungie/{discordId}/{service}",
            [Authorize(AuthenticationSchemes = BungieNetAuthenticationDefaults.AuthenticationScheme)]
            (HttpContext httpContext, ulong discordId, string service) =>
            {
                switch (service)
                {
                    case "felicity":
                    case "lostsector":
                        lock (AuthCache)
                        {
                            AuthCache.Add(discordId, service);
                        }

                        break;
                    default:
                        Results.BadRequest("Unknown service type.");
                        break;
                }

                const string cookieName = ".AspNetCore.Cookies";
                var existingCookie = httpContext.Request.Cookies[cookieName];

                if (existingCookie != null)
                    httpContext.Response.Cookies.Append(cookieName, existingCookie, new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(-1)
                    });

                return httpContext.ChallengeAsync(
                    "BungieNet",
                    new AuthenticationProperties
                    {
                        RedirectUri = $"auth/bungie/{discordId}/post_callback"
                    });
            });

        group.MapGet("/bungie/{discordId}/post_callback",
            async (HttpContext httpContext, DbManager db, ulong discordId) =>
            {
                string? service;

                lock (AuthCache)
                {
                    AuthCache.TryGetValue(discordId, out service);
                }

                if (string.IsNullOrEmpty(service))
                    return Results.BadRequest(
                        "User not found in cache. Please try running the register command again.");

                var claim = httpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
                if (claim is null)
                    return TypedResults.Redirect("https://tryfelicity.one/auth_failure", true);

                var id = long.Parse(claim.Value);
                if (!BungieAuthCacheService.GetByIdAndRemove(id, out var context))
                    return TypedResults.Redirect("https://tryfelicity.one/auth_failure", true);

                var token = context.Token;

                var nowTime = DateTime.UtcNow;
                var baseTime = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day,
                    nowTime.Hour, nowTime.Minute, nowTime.Second);

                var user = await db.Users.Include(user => user.BungieProfiles)
                    .FirstOrDefaultAsync(x => x.Id == discordId);
                var addUser = false;
                var addBungieUser = false;

                if (user == null)
                {
                    addUser = true;

                    user = new User
                    {
                        Id = discordId
                    };
                }

                switch (service)
                {
                    case "felicity":
                        user.RegisteredFelicity = true;
                        break;
                    case "lostsector":
                        user.RegisteredLostSector = true;
                        break;
                }

                lock (AuthCache)
                {
                    AuthCache.Remove(discordId);
                }

                BungieProfile? bungieUser;
                var bungieUsers = user.BungieProfiles;

                if (bungieUsers.Count == 0)
                {
                    addBungieUser = true;

                    bungieUser = new BungieProfile
                    {
                        UserId = discordId,
                        MembershipId = token.MembershipId
                    };
                }
                else
                {
                    bungieUser = bungieUsers.FirstOrDefault(x => x.MembershipId == token.MembershipId);

                    if (bungieUser == null)
                    {
                        addBungieUser = true;
                        bungieUser = new BungieProfile { UserId = discordId, MembershipId = token.MembershipId };
                    }
                }

                bungieUser.OauthToken = token.AccessToken;
                bungieUser.RefreshToken = token.RefreshToken;
                bungieUser.TokenExpires = baseTime.AddSeconds(token.ExpiresIn);
                bungieUser.RefreshExpires = baseTime.AddSeconds(token.RefreshExpiresIn);

                if (addUser) db.Users.Add(user);

                if (addBungieUser) db.BungieProfiles.Add(bungieUser);

                await db.SaveChangesAsync();

                return TypedResults.Redirect("https://tryfelicity.one/auth_success", true);
            });
    }
}
