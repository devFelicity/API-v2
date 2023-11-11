using API.Responses;
using API.Services;
using API.Util;
using DotNetBungieAPI.Service.Abstractions;

namespace API.Routes;

#pragma warning disable IDE0300

public static class StatusRoute
{
    private static readonly ILogger Logger = Logging.CreateLogger("StatusRoute");

    public static void MapStatus(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (HttpContext httpContext, IBungieClient bungieClient) =>
        {
            var statusResponse = new Status
            {
                ErrorCode = ErrorCode.Success,
                ErrorStatus = "Success",
                Message = "Felicity.Api.Status",
                Response = new StatusResponse[]
                {
                    new()
                    {
                        Name = "Felicity.Api",
                        Alive = IsFelicityApiAlive(httpContext)
                    },
                    new()
                    {
                        Name = "Felicity",
                        Alive = IsFelicityAlive()
                    },
                    new()
                    {
                        Name = "Bungie",
                        Alive = await IsBungieAlive(bungieClient)
                    }
                }
            };

            return TypedResults.Json(statusResponse, Common.JsonSerializerOptions);
        });
    }

    private static bool IsFelicityApiAlive(HttpContext httpContext)
    {
        try
        {
            using var client = new HttpClient();
            var response = client.GetAsync($"{httpContext.Request.Scheme}://{httpContext.Request.Host}/health").Result;
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            if (Variables.Environment == Environment.Development)
                Logger.LogError(ex, "[StatusRoute] {service} is not healthy", "Felicity.Api");

            return false;
        }
    }

    private static bool IsFelicityAlive()
    {
        try
        {
            using var client = new HttpClient();
            var response = client.GetAsync("http://felicity/health").Result;
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            if (Variables.Environment == Environment.Development)
                Logger.LogError(ex, "[StatusRoute] {service} is not healthy", "Felicity");

            return false;
        }
    }

    private static async Task<bool> IsBungieAlive(IBungieClient bungieClient)
    {
        try
        {
            return await BungieTools.IsApiUp(bungieClient);
        }
        catch (Exception ex)
        {
            if (Variables.Environment == Environment.Development)
                Logger.LogError(ex, "[StatusRoute] {service} is not healthy", "Bungie");

            return false;
        }
    }
}
