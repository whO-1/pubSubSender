using System.Text.Json.Serialization;

namespace SimpleService.Models;

public record PubSubPushRequest(
    [property: JsonPropertyName("message")] PubSubPushMessage Message, 
    [property: JsonPropertyName("subscription")] string Subscription);

public record PubSubPushMessage(
    [property: JsonPropertyName("data")] string Data, 
    [property: JsonPropertyName("messageId")] string MessageId, 
    [property: JsonPropertyName("attributes")] Dictionary<string, string>? Attributes, 
    [property: JsonPropertyName("publishTime")] string PublishTime);
