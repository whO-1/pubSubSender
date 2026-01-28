using Google.Cloud.PubSub.V1;

namespace SimpleService.PubSub
{
	public interface IPublisherFactory
	{
		Task<IPublisher?> GetPublisherAsync(string topicId, PublisherClient.Settings? settings = null);
	}
}
