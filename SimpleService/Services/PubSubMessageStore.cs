using System.Collections.Concurrent;

namespace SimpleService.Services;

public record PubSubReceivedMessage(string MessageId, string Data, IReadOnlyDictionary<string, string> Attributes, DateTimeOffset ReceivedAt);

public class PubSubMessageStore
{
	private const int MaxMessages = 50;
	private readonly ConcurrentQueue<PubSubReceivedMessage> _messages = new();

	public void Add(PubSubReceivedMessage message)
	{
		_messages.Enqueue(message);
		while (_messages.Count > MaxMessages && _messages.TryDequeue(out _))
		{
		}
	}

	public IReadOnlyCollection<PubSubReceivedMessage> GetAll() => _messages.ToArray();
}
