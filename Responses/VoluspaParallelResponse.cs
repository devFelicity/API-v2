using System.Text.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace API.Responses;

public class Voluspa
{
    public class VoluspaResponse
    {
        public ErrorCode ErrorCode { get; set; } = ErrorCode.UnknownError;

        public string ErrorStatus { get; set; } = "Unknown Error";

        public string Message { get; set; } = "Unknown";

        public ParallelResponse[]? Response { get; set; }
    }

    public class ParallelResponse
    {
        public string Name { get; set; } = "Unknown Project";

        public string Description { get; set; } = "Unknown Project";

        public Uri? Url { get; set; }

        public Member[]? Members { get; set; }
    }

    public class Member
    {
        public string MembershipId { get; set; } = "Unknown Member";

        public long MembershipType { get; set; } = 0;
    }

    public class ParallelQuery
    {
        public static async Task<VoluspaResponse> GetResponse()
        {
            var response = await new HttpClient().GetAsync("https://b.vlsp.network/Public/ParallelPrograms");

            // ReSharper disable once InvertIf
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<VoluspaResponse>(await response.Content.ReadAsStringAsync(),
                    Common.JsonSerializerOptions);
                if (result != null)
                    return result;
            }

            return new VoluspaResponse
            {
                ErrorCode = ErrorCode.QueryFailed,
                ErrorStatus = "Internal Server Error",
                Message = "Could not retrieve project list from Voluspa API."
            };
        }
    }
}
