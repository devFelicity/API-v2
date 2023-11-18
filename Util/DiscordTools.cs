using Discord.Webhook;

namespace API.Util;

public static class DiscordTools
{
    public enum WebhookChannel
    {
        General,
        Vendors,
        Logs
    }

    private static DiscordWebhookClient? WebhookClient { get; set; }

    private static Dictionary<WebhookChannel, string> WebhookUrls { get; set; } = new();

    public static void Initialize(ConfigurationManager builderConfiguration)
    {
        var generalWebhookUrl = builderConfiguration["Discord:Webhooks:General"];
        var logsWebhookUrl = builderConfiguration["Discord:Webhooks:Logs"];
        var vendorsWebhookUrl = builderConfiguration["Discord:Webhooks:Vendors"];

        if (generalWebhookUrl == null || logsWebhookUrl == null || vendorsWebhookUrl == null)
            throw new NullReferenceException("Discord:Webhooks is null");

        WebhookUrls = new Dictionary<WebhookChannel, string>
        {
            { WebhookChannel.General, generalWebhookUrl },
            { WebhookChannel.Vendors, vendorsWebhookUrl },
            { WebhookChannel.Logs, logsWebhookUrl }
        };
    }

    public static async Task SendMessage(WebhookChannel channel, string message)
    {
        if (!WebhookUrls.TryGetValue(channel, out var value))
            throw new NullReferenceException($"Discord:Webhooks:{channel} is null");

        WebhookClient = new DiscordWebhookClient(value);

        await WebhookClient.SendMessageAsync(message);

        WebhookClient = null;
    }
}
