using API.Responses;

namespace API.Routes;

public static class ManifestRoute
{
    public static void MapManifest(this RouteGroupBuilder group)
    {
        group.MapGet("/", () =>
        {
            var response = new ManifestResponse();
            if (string.IsNullOrEmpty(Variables.ManifestVersion))
            {
                response.ErrorStatus = "No manifest loaded.";
                response.ErrorCode = ErrorCode.QueryFailed;
                response.Message = "Felicity.Api.Manifest";

                return TypedResults.Json(response, Common.JsonSerializerOptions);
            }

            response.Response = Variables.ManifestVersion;
            response.ErrorStatus = "Success";
            response.ErrorCode = ErrorCode.Success;
            response.Message = "Felicity.Api.Manifest";

            return TypedResults.Json(response, Common.JsonSerializerOptions);
        });
    }
}
