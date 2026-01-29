using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SimpleService.Models;

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
		_logger.LogInformation("PubSubTraceContextMiddleware: Processing request {Path} {Method}", 
			context.Request.Path, 
			context.Request.Method);
		
		if (context.Request.Path.Value?.EndsWith("/push", StringComparison.OrdinalIgnoreCase) == true &&
		    context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("PubSubTraceContextMiddleware: Matched /push endpoint");
			
			try
			{
				context.Request.EnableBuffering();

				using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
				var body = await reader.ReadToEndAsync();
				context.Request.Body.Position = 0;
				
				_logger.LogInformation("PubSubTraceContextMiddleware: Body read, length: {Length}, raw body: {Body}", body.Length, body);

				var pushRequest = System.Text.Json.JsonSerializer.Deserialize<PubSubPushRequest>(body, new System.Text.Json.JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});
				
				_logger.LogInformation("PubSubTraceContextMiddleware: Deserialized message, has attributes: {HasAttributes}, count: {Count}", 
					pushRequest?.Message?.Attributes != null, 
					pushRequest?.Message?.Attributes?.Count ?? 0);

				if (pushRequest?.Message?.Attributes != null && pushRequest.Message.Attributes.Count > 0)
				{
					_logger.LogInformation("PubSubTraceContextMiddleware: Message attributes: {@Attributes}", pushRequest.Message.Attributes);
					
					var propagator = new CompositeTextMapPropagator(
						new TextMapPropagator[] {
							new TraceContextPropagator(),
							new BaggagePropagator()
						}
					);

					var parentContext = propagator.Extract(
						default,
						pushRequest.Message.Attributes,
						(carrier, key) =>
							carrier.TryGetValue(key, out var value)
								? new[] { value }
								: Array.Empty<string>());

					_logger.LogInformation("PubSubTraceContextMiddleware: Extracted context - TraceId: {TraceId}, SpanId: {SpanId}", 
						parentContext.ActivityContext.TraceId,
						parentContext.ActivityContext.SpanId);

					if (parentContext.ActivityContext.TraceId != default)
					{
						if (!context.Request.Headers.ContainsKey("traceparent"))
						{
							var traceparent = $"00-{parentContext.ActivityContext.TraceId}-{parentContext.ActivityContext.SpanId}-01";
							context.Request.Headers["traceparent"] = traceparent;
							_logger.LogInformation("PubSubTraceContextMiddleware: Injected traceparent header: {Traceparent}", traceparent);
						}
						else
						{
							_logger.LogWarning("PubSubTraceContextMiddleware: traceparent header already exists");
						}
						
						Baggage.Current = parentContext.Baggage;
						
						context.Items["PubSubTraceContext"] = parentContext;
					}
					else
					{
						_logger.LogWarning("PubSubTraceContextMiddleware: Extracted TraceId is default/empty");
					}
				}
				else
				{
					_logger.LogWarning("PubSubTraceContextMiddleware: Message has no attributes or is null");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "PubSubTraceContextMiddleware: Error extracting trace context from Pub/Sub message");
			}
		}
		else
		{
			_logger.LogInformation("PubSubTraceContextMiddleware: Not a /push endpoint, skipping");
		}

		await _next(context);
		
		// After the request, check if trace was properly propagated
		if (context.Items.ContainsKey("PubSubTraceContext"))
		{
			var expectedContext = (PropagationContext)context.Items["PubSubTraceContext"];
			var actualTraceId = Activity.Current?.TraceId.ToString();
			_logger.LogInformation(
				"PubSubTraceContextMiddleware: Trace propagation result - Expected: {ExpectedTraceId}, Actual: {ActualTraceId}, Match: {Match}",
				expectedContext.ActivityContext.TraceId,
				actualTraceId,
				expectedContext.ActivityContext.TraceId.ToString() == actualTraceId);
		}
	}
}
