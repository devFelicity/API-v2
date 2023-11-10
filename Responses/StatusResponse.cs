// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace API.Responses;

public class Status
{
    public ErrorCode ErrorCode { get; set; } = ErrorCode.UnknownError;

    public string ErrorStatus { get; set; } = "Unknown Error";

    public string Message { get; set; } = "Unknown";

    public StatusResponse[]? Response { get; set; }
}

public class StatusResponse
{
    public string Name { get; set; } = "Unknown Monitor";

    public bool Alive { get; set; }
}
