using API.Contexts;

namespace API.Routes;

public class UserRoute(DbManager db)
{
    private readonly DbManager _db = db;

    public int GetCount()
    {
        return _db.Users.Count();
    }
}