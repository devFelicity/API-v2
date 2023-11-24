// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace API.Responses;

public class ListResponse
{
    public ErrorCode ErrorCode { get; set; } = ErrorCode.UnknownError;

    public string ErrorStatus { get; set; } = "Unknown Error";

    public string Message { get; set; } = "Unknown";

    public List<object> Response { get; set; } = [];
}
