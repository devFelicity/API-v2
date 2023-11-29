using System.Security.Claims;
using System.Text.Json;
using API.Contexts;
using API.Contexts.Objects;
using API.Responses;
using API.Services;
using API.Util;
using DotNetBungieAPI.AspNet.Security.OAuth.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

// ReSharper disable RouteTemplates.RouteParameterIsNotPassedToMethod

namespace API.Routes;

public static class AuthRoute
{
    private static readonly TimedDictionary<long, string> AuthCache = new(TimeSpan.FromMinutes(2));

    public static void MapAuth(this RouteGroupBuilder group)
    {
#pragma warning disable ASP0018
        group.MapGet("/bungie/{discordId}/{service}",
#pragma warning restore ASP0018
            [Authorize(AuthenticationSchemes = BungieNetAuthenticationDefaults.AuthenticationScheme)]
            async (context) =>
            {
                if (!ulong.TryParse(context.Request.RouteValues["discordId"]!.ToString(), out var discordId))
                {
                    var response = new UserResponse
                    {
                        ErrorCode = ErrorCode.InvalidParameters,
                        ErrorStatus = "Unknown Discord ID.",
                        Message = "Felicity.Api.Auth"
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response,
                        Common.JsonSerializerOptions));
                    return;
                }

                var userId = UserExtensions.SignId(discordId);

                var service = context.Request.RouteValues["service"]!.ToString();

                switch (service)
                {
                    case "felicity":
                    case "lostsector":
                        lock (AuthCache)
                        {
                            AuthCache.Add(userId, service);
                        }

                        break;
                    default:
                        var response = new UserResponse
                        {
                            ErrorCode = ErrorCode.InvalidParameters,
                            ErrorStatus = "Unknown service type.",
                            Message = "Felicity.Api.Auth"
                        };
                        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
                            Common.JsonSerializerOptions));
                        return;
                }

                await context.ChallengeAsync(
                    "BungieNet",
                    new AuthenticationProperties
                    {
                        RedirectUri = $"auth/bungie/post_callback/{discordId}"
                    });
            });

        group.MapGet("/bungie/post_callback/{discordId}",
            async (HttpContext httpContext, DbManager db, ulong discordId) =>
            {
                string? service;
                var userId = UserExtensions.SignId(discordId);

                lock (AuthCache)
                {
                    AuthCache.TryGetValue(userId, out service);
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

                var user = await db.Users.Include(user => user.BungieProfiles)
                    .FirstOrDefaultAsync(x => x.Id == userId);
                var addUser = false;
                var addBungieUser = false;

                if (user == null)
                {
                    addUser = true;

                    user = new User
                    {
                        Id = userId
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
                    AuthCache.Remove(userId);
                }

                BungieProfile? bungieUser;
                var bungieUsers = user.BungieProfiles;

                if (bungieUsers.Count == 0)
                {
                    addBungieUser = true;

                    bungieUser = new BungieProfile
                    {
                        UserId = userId,
                        MembershipId = token.MembershipId
                    };
                }
                else
                {
                    bungieUser = bungieUsers.FirstOrDefault(x => x.MembershipId == token.MembershipId);

                    if (bungieUser == null)
                    {
                        addBungieUser = true;
                        bungieUser = new BungieProfile { UserId = userId, MembershipId = token.MembershipId };
                    }
                }

                bungieUser.OauthToken = token.AccessToken;
                bungieUser.RefreshToken = token.RefreshToken;
                bungieUser.TokenExpires = nowTime.AddSeconds(token.ExpiresIn);
                bungieUser.RefreshExpires = nowTime.AddSeconds(token.RefreshExpiresIn);

                if (addUser) db.Users.Add(user);

                if (addBungieUser) db.BungieProfiles.Add(bungieUser);

                await db.SaveChangesAsync();

                return TypedResults.Redirect("https://tryfelicity.one/auth_success", false, true);
            });
    }
}
