using API.Contexts;
using API.Responses;
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

            return TypedResults.Ok(response);
        });

        group.MapDelete("/remove/{discordId}", async (DbManager db, ulong discordId) =>
        {
            var response = new UserResponse();

            var targetUser = db.Users.Include(x => x.BungieProfiles).FirstOrDefault(x => x.Id == discordId);

            if (targetUser == null)
            {
                response.ErrorCode = ErrorCode.QueryFailed;
                response.ErrorStatus = "User not found.";
                response.Message = "Felicity.Api.User";

                return TypedResults.Ok(response);
            }

            if (targetUser.BungieProfiles.Count > 0)
                db.BungieProfiles.RemoveRange(targetUser.BungieProfiles);

            db.Users.Remove(targetUser);

            await db.SaveChangesAsync();

            response.ErrorCode = ErrorCode.Success;
            response.ErrorStatus = "Success";
            response.Message = "Felicity.Api.User";

            return TypedResults.Ok(response);
        });
    }
}
