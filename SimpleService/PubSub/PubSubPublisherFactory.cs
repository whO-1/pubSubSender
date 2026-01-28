using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SimpleService.PubSub
{
	internal class PubSubPublisherFactory(IOptions<PubSubSettings>? options, IMemoryCache memoryCache, ILoggerFactory loggerFactory) : IPublisherFactory
	{
		private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		private readonly IMemoryCache _publisherCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		private readonly PubSubSettings _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

		public async Task<IPublisher?> GetPublisherAsync(string topicId, PublisherClient.Settings? settings = null)
		{
			var logger = _loggerFactory.CreateLogger<PubSubPublisherFactory>();

			if (string.IsNullOrWhiteSpace(_settings.GcpProjectId))
			{
				logger.LogError("[{MethodName}] {Property} is not configured.", nameof(GetPublisherAsync), nameof(PubSubSettings.GcpProjectId));
				return null;
			}

			return await _publisherCache.GetOrCreateAsync(topicId, async cacheEntry =>
			{
				cacheEntry.SlidingExpiration = TimeSpan.FromMinutes(15);
				cacheEntry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
				{
					EvictionCallback = OnPublisherRemoved
				});

				var publisherClient = await CreatePublisherClientAsync(logger, topicId, _settings.GcpProjectId, settings);
				var publisherLogger = _loggerFactory.CreateLogger<PubSubPublisher>();
				return new PubSubPublisher(publisherClient, publisherLogger, topicId);
			});
		}

		private async Task<PublisherClient> CreatePublisherClientAsync(ILogger logger, string topicId, string gcpProjectId, PublisherClient.Settings? settings = null)
		{
			var publisherClientBuilder = new PublisherClientBuilder
			{
				TopicName = new TopicName(gcpProjectId, topicId),
				Logger = logger,
				Settings = settings
			};

			if (!string.IsNullOrWhiteSpace(_settings.EmulatorEndpoint)){
				logger.LogInformation("Using PubSub emulator at endpoint {Endpoint} for GCP project ID: {ProjectId}", _settings.EmulatorEndpoint, gcpProjectId);
				publisherClientBuilder.EmulatorDetection = EmulatorDetection.EmulatorOrProduction;
				publisherClientBuilder.Endpoint = _settings.EmulatorEndpoint;
			}

			return await publisherClientBuilder.BuildAsync();
		}

		private static async void OnPublisherRemoved(object key, object? value, EvictionReason reason, object? state)
		{
			try
			{
				if (value is IPublisher publisher)
				{
					await publisher.DisposeAsync();
				}
			}
			catch (Exception e)
			{
				throw; // TODO handle exception
			}
		}
	}
}
