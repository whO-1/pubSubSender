using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace SimpleService.PubSub
{
	public sealed class PubSubPublisher : IPublisher
	{
		private readonly JsonSerializerOptions _serializerOptions;
		private readonly ILogger _logger;
		private readonly PublisherClient _publisherClient;
		private readonly string _topic;

		public PubSubPublisher(PublisherClient publisherClient, ILogger<PubSubPublisher> logger, string topic)
		{
			_serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
			{
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				Converters = { new JsonStringEnumConverter() }
			};
			_publisherClient = publisherClient ?? throw new ArgumentNullException(nameof(publisherClient), $"Parameter {publisherClient} cant be null.");
			_logger = logger ?? throw new ArgumentNullException(nameof(logger), $"Parameter {logger} cant be null.");
			_topic = topic ?? throw new ArgumentNullException(nameof(topic), $"Parameter {topic} cant be null.");
			_logger.LogInformation("[{MethodName}] PubSubPublisher for topic: {@TopicName} - created", nameof(PubSubPublisher), _publisherClient.TopicName);
		}

		public void PublishMessage<T>(string tenantId, T message, IDictionary<string, string> attributes)
		{
			byte[] serializedMessage = SerializeMessage(message);
			var messagePayload = ByteString.CopyFrom(serializedMessage);
			PublishPubSubMessage(tenantId, messagePayload, attributes);
		}

		public void PublishMessage(string tenantId, byte[] serializedMessage, IDictionary<string, string> attributes)
		{
			var messagePayload = ByteString.CopyFrom(serializedMessage);
			PublishPubSubMessage(tenantId, messagePayload, attributes);
		}

		private void PublishPubSubMessage(string tenantId, ByteString messagePayload, IDictionary<string, string>? attributes)
		{
			try
			{
				var enrichedAttributes = EnrichAttributesWithTrace(attributes);
			
			_logger.LogInformation("[{MethodName}] After enrichment - count: {Count}, attributes: {@EnrichedAttributes}", 
				nameof(PublishPubSubMessage), enrichedAttributes?.Count ?? 0, enrichedAttributes);

				var pubSubMessage = new PubsubMessage
				{
					Data = messagePayload
				};

				if (enrichedAttributes is not null && enrichedAttributes.Count > 0)
				{
					pubSubMessage.Attributes.Add(enrichedAttributes);
					_logger.LogInformation("[{MethodName}] Added to PubsubMessage: {@Attrs}", 
						nameof(PublishPubSubMessage), pubSubMessage.Attributes);
				}
				else
				{
					_logger.LogWarning("[{MethodName}] No attributes", nameof(PublishPubSubMessage));
				}

				// The call should not be awaited, read more here:
				// https://github.com/googleapis/google-cloud-dotnet/issues/5893#issuecomment-770914214
				PublishAsync(pubSubMessage).ConfigureAwait(false);

				_logger.LogInformation("[{MethodName}] topic: {TopicName}, message: {PubSubMessagePayload}, attributes: {@Attributes}, tenant: {TenantId}",
						nameof(PublishPubSubMessage), _topic, messagePayload.ToStringUtf8(), enrichedAttributes, tenantId);
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Error publishing message to topic {TopicName} for tenant {TenantId}", _topic, tenantId);
				throw new InvalidOperationException(exception.Message, exception);
			}
		}

		private static Dictionary<string, string>? EnrichAttributesWithTrace(IDictionary<string, string>? attributes)
		{
			var result = attributes is null
				? new Dictionary<string, string>()
				: new Dictionary<string, string>(attributes);

			var activity = Activity.Current;
			if (activity is null)
			{
				return result;
			}
			
			var propagator = new CompositeTextMapPropagator(
				new TextMapPropagator[] {
					new TraceContextPropagator(),
					new BaggagePropagator()
				}
			);
			propagator.Inject(
				new PropagationContext(activity.Context, Baggage.Current),
				result,
				static (carrier, key, value) =>
				{
					carrier[key] = value;
				});

			return result;
		}

		public async ValueTask DisposeAsync()
		{
			try
			{
				await ShutdownAsync(TimeSpan.FromSeconds(3));
				_logger.LogInformation("[{MethodName}] Publisher for topic: {@TopicName} - shutdown", nameof(DisposeAsync), _publisherClient.TopicName);
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "[{MethodName}] Failed to shutdown: {@TopicName} - shutdown", nameof(DisposeAsync), _publisherClient?.TopicName);
			}
			finally
			{
				GC.SuppressFinalize(this);
			}
		}

		private Task<string> PublishAsync(PubsubMessage message)
		{
			return _publisherClient.PublishAsync(message);
		}

		private Task ShutdownAsync(TimeSpan timeout)
		{
			return _publisherClient.ShutdownAsync(timeout);
		}

		private byte[] SerializeMessage<T>(T message)
		{
			return JsonSerializer.SerializeToUtf8Bytes(message, _serializerOptions);
		}
	}
}
