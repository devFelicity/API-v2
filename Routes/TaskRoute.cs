using API.Responses;
using API.Services;

namespace API.Routes;

public static class TaskRoute
{
    public static void MapTasks(this RouteGroupBuilder group)
    {
        group.MapGet("/", () =>
        {
            var response = new ListResponse
            {
                ErrorStatus = "Success",
                ErrorCode = ErrorCode.Success,
                Message = "Felicity.Api.TaskScheduler"
            };

            foreach (var task in TaskSchedulerService.Tasks)
                response.Response.Add(new
                {
                    task.Name,
                    task.LastRun
                });

            return TypedResults.Json(response, Common.JsonSerializerOptions);
        });
    }
}
