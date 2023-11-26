using API.Contexts;
using API.Responses;
using API.Util;
using DotNetBungieAPI.Service.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace API.Routes;

public static class UserRoute
{
    public static void MapUsers(this RouteGroupBuilder group)
    {
        group.MapGet("/all", (DbManager db) =>
        {
            var response = new UserResponse
            {
                ErrorCode = ErrorCode.Success,
                ErrorStatus = "Success",
                Message = "Felicity.Api.User",
                Response = [.. db.Users]
            };

            return TypedResults.Json(response, Common.JsonSerializerOptions);
        });

        group.MapGet("/full/{discordId}",
            async (HttpContext context, DbManager db, IBungieClient bungieClient, ulong discordId) =>
            {
                var response = new UserResponse
                {
                    Message = "Felicity.Api.User"
                };

                if (!context.IsAuthorized())
                {
                    response = new UserResponse
                    {
                        ErrorCode = ErrorCode.NotAuthorized,
                        ErrorStatus = "Request not authorized."
                    };
                    return TypedResults.Json(response, Common.JsonSerializerOptions);
                }

                var user = db.Users.Include(u => u.BungieProfiles)
                    .FirstOrDefault(x => x.Id == UserExtensions.SignId(discordId));

                if (user == null)
                {
                    response.ErrorCode = ErrorCode.QueryFailed;
                    response.ErrorStatus = "User not found.";

                    return TypedResults.Json(response, Common.JsonSerializerOptions);
                }

                if (user.BungieProfiles.First().DestinyMembershipId == 0)
                    await user.BungieProfiles.First().UpdateMembership(bungieClient);

                response.ErrorCode = ErrorCode.Success;
                response.ErrorStatus = "Success";
                response.Response = [user];

                return TypedResults.Json(response, Common.JsonSerializerOptions);
            });

        group.MapDelete("/remove/{discordId}", async (HttpContext context, DbManager db, ulong discordId) =>
        {
            var response = new UserResponse
            {
                Message = "Felicity.Api.User"
            };

            if (!context.IsAuthorized())
            {
                response = new UserResponse
                {
                    ErrorCode = ErrorCode.NotAuthorized,
                    ErrorStatus = "Request not authorized."
                };
                return TypedResults.Json(response, Common.JsonSerializerOptions);
            }

            var userId = UserExtensions.SignId(discordId);

            var targetUser = db.Users
                .Include(x => x.BungieProfiles)
                .FirstOrDefault(x => x.Id == userId);

            if (targetUser == null)
            {
                response.ErrorCode = ErrorCode.QueryFailed;
                response.ErrorStatus = "User not found.";

                return TypedResults.Json(response, Common.JsonSerializerOptions);
            }

            if (db.UserBans.Any(x => x.User == targetUser))
            {
                await DiscordTools.SendMessage(DiscordTools.WebhookChannel.Logs,
                    $"Banned user {targetUser.Id} tried deleting profile.");

                response.ErrorCode = ErrorCode.QueryFailed;
                response.ErrorStatus = "User is banned.";
            }
            else
            {
                db.Users.Remove(targetUser);

                await db.SaveChangesAsync();

                response.ErrorCode = ErrorCode.Success;
                response.ErrorStatus = "Success";
            }

            return TypedResults.Json(response, Common.JsonSerializerOptions);
        });
    }
}
