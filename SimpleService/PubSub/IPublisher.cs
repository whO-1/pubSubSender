namespace SimpleService.PubSub
{
	public interface IPublisher : IAsyncDisposable
	{
		void PublishMessage<T>(string tenantId, T message, IDictionary<string, string> attributes);
		void PublishMessage(string tenantId, byte[] serializedMessage, IDictionary<string, string> attributes);
	}
}
