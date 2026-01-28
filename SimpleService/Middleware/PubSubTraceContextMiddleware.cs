using System.Diagnostics;
using Google.Cloud.PubSub.V1;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace SimpleService.Middleware;

public class PubSubTraceContextMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<PubSubTraceContextMiddleware> _logger;

	public PubSubTraceContextMiddleware(RequestDelegate next, ILogger<PubSubTraceContextMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (context.Request.Path.Value?.EndsWith("/push", StringComparison.OrdinalIgnoreCase) == true)
		{
			try
			{
				context.Request.EnableBuffering();

				using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
				var body = await reader.ReadToEndAsync();
				context.Request.Body.Position = 0; 

				var message = System.Text.Json.JsonSerializer.Deserialize<PubsubMessage>(body);

				if (message?.Attributes != null && message.Attributes.Count > 0)
				{
					var propagator = new CompositeTextMapPropagator(
						new TextMapPropagator[] {
							new TraceContextPropagator(),
							new BaggagePropagator()
						}
					);

					var parentContext = propagator.Extract(
						default,
						message.Attributes,
						(carrier, key) =>
							carrier.TryGetValue(key, out var value)
								? new[] { value }
								: Array.Empty<string>());

					Baggage.Current = parentContext.Baggage;
					
					var currentActivity = Activity.Current;
					
					// Baggage.Current = parentContext.Baggage;
					//
					// var activitySource = new ActivitySource("My First Project");
					// using var activity = activitySource.StartActivity(
					// 	"My First Project",
					// 	ActivityKind.Consumer,
					// 	parentContext.ActivityContext);
					
					
					if (currentActivity != null && parentContext.ActivityContext.TraceId != default)
					{
						currentActivity.SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId);
						
						_logger.LogInformation(
							"Extracted trace context from Pub/Sub message - TraceId: {TraceId}, SpanId: {SpanId}",
							parentContext.ActivityContext.TraceId,
							parentContext.ActivityContext.SpanId);
					}
					else
					{
						_logger.LogWarning("No valid trace context found in Pub/Sub message attributes");
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error extracting trace context from Pub/Sub message");
			}
		}

		await _next(context);
	}
}
