using DotNetBungieAPI.Service.Abstractions;

namespace API.Util;

public static class BungieTools
{
    public static async Task<bool> IsApiUp(IBungieClient bungieClient)
    {
        var response = await bungieClient.ApiAccess.Misc.GetCommonSettings();
        return response.IsSuccessfulResponseCode && response.Response.Systems["Destiny2"].IsEnabled;
    }
}
