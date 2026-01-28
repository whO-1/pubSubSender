namespace SimpleService.Models;

public record PubSubPublishRequest(string Topic, string Message, Dictionary<string, string>? Attributes);
