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

					if (parentContext.ActivityContext.TraceId != default)
					{
						// Inject the extracted trace context into HTTP headers so ASP.NET Core picks it up
						context.Request.Headers["traceparent"] = $"00-{parentContext.ActivityContext.TraceId}-{parentContext.ActivityContext.SpanId}-01";
						
						Baggage.Current = parentContext.Baggage;
						
						_logger.LogInformation(
							"Injected trace context from Pub/Sub message into headers - TraceId: {TraceId}, SpanId: {SpanId}",
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
