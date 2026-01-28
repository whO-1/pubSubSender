namespace SimpleService.Models;

public record PubSubPushRequest(PubSubPushMessage Message, string Subscription);

public abstract record PubSubPushMessage(string Data, string MessageId, Dictionary<string, string>? Attributes, string PublishTime);
