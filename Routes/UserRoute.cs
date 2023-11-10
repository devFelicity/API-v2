using API.Contexts;

namespace API.Routes;

public static class UserRoute
{
    public static void MapUsers(this RouteGroupBuilder group)
    {
        group.MapGet("/test", (DbManager db) => Task.FromResult(TypedResults.Ok(db.Users.OrderBy(x => x.Id).First())));
    }
}
