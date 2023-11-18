// ReSharper disable UnusedMember.Global

using System.Text.Json;

namespace API.Responses;

public enum ErrorCode : long
{
    UnknownError = 0,
    Success = 1,
    QueryFailed = 400,
    NotAuthorized = 401,
    InvalidParameters = 1000,
    ParameterMissing = 1001,
    ParameterInvalid = 1002,
    ParameterEmpty = 1003,
    ParameterTooLong = 1004,
    ParameterTooShort = 1005,
    CatastrophicExplosionHolyShitTheServerIsOnFire = 9001
}

public static class Common
{
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };
}
