using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleService.PubSub;

public class PubSubMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    public int DeliveryAttempt { get; set; }
    public Msg Message { get; set; }
    public string? Subscription { get; set; }

    public string DecodeMessageData() => DecodeBase64String(Message.Data);
    public T? DeserializeMessageData<T>()
    {
        string data = DecodeMessageData();
        return JsonSerializer.Deserialize<T>(data, SerializerOptions);
    }

    public class Msg
    {
        public Dictionary<string, string> Attributes { get; set; } = [];
        public string? Data { get; set; }
        public string MessageId { get; set; }
        public string? OrederingKey { get; set; }
        public DateTime? PublishTime { get; set; }
    }

    private static string DecodeBase64String(string base64EncodedString)
    {
        if (string.IsNullOrEmpty(base64EncodedString))
            return string.Empty;

        byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedString);
        string decodedString = Encoding.UTF8.GetString(base64EncodedBytes);
        return decodedString;
    }
}