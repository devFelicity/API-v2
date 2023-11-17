using System.Security.Claims;
using API.Contexts;
using API.Contexts.Objects;
using API.Services;
using DotNetBungieAPI.AspNet.Security.OAuth.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace API.Routes;

public static class AuthRoute
{
    public static void MapAuth(this RouteGroupBuilder group)
    {
        group.MapGet("/bungie/{discordId}",
            [Authorize(AuthenticationSchemes = BungieNetAuthenticationDefaults.AuthenticationScheme)]
            async (HttpContext httpContext, ulong discordId) =>
            {
                await httpContext.ChallengeAsync(
                    "BungieNet",
                    new AuthenticationProperties
                    {
                        RedirectUri = $"auth/bungie/{discordId}/post_callback/"
                    });
            });

        group.MapGet("/bungie/{discordId}/post_callback",
            async (HttpContext httpContext, DbManager db, ulong discordId) =>
            {
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
                        Id = discordId,
                        RegisteredFelicity = true,
                        RegisteredLostSector = false
                    };
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
