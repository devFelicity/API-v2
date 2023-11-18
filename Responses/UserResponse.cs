// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using API.Contexts.Objects;

namespace API.Responses;

public class UserResponse
{
    public ErrorCode ErrorCode { get; set; } = ErrorCode.UnknownError;

    public string ErrorStatus { get; set; } = "Unknown Error";

    public string Message { get; set; } = "Unknown";

    public User[]? Response { get; set; }
}
