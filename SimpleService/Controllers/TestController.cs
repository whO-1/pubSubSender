using System.Diagnostics;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Encoding = System.Text.Encoding;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SimpleService.PubSub;

namespace SimpleService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController(ILogger<TestController> logger, HttpClient httpClient, IPublisherFactory publisherFactory) : ControllerBase
{
    [HttpPost("[action]")]
    public async Task<ActionResult> CallExternalService()
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://pubsub-subscriber-1075558457930.europe-west1.run.app/api/test/traceidpropagation/push");

            if (Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader))
            {
                request.Headers.TryAddWithoutValidation(
                    HeaderNames.Authorization,
                    authHeader.ToString());
            }

            request.Content = new StringContent(
                "{\"message\":\"hello\"}",
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.SendAsync(request);

            logger.LogInformation(
                "Downstream responded with {StatusCode}",
                response.StatusCode);

            return Ok(new { status = response.StatusCode });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error publishing");
            return BadRequest();
        }
    }
    
    [HttpPost("[action]")]
    public async Task<ActionResult> PublishMessageWithTraceId()
    {
        var message = new PubsubMessage
        {
            Attributes = { { "Tenant-Id", "tenantId" } },
            MessageId = "testId",
            Data = ByteString.CopyFromUtf8("hello world"),
            OrderingKey = "data"
        };
        
        var attributes = new Dictionary<string, string>();
        try
        {
            var publisher = await publisherFactory.GetPublisherAsync("test-topic");
            publisher?.PublishMessage("testTenantId", message, attributes);
            return Ok("Successfully published!");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error publishing");
            return BadRequest();
        }
    }
    
    [HttpPost("[action]/push")]
    public IActionResult TraceIdPropagation([FromBody] PubsubMessage message)
    {
        // var propagator = new CompositeTextMapPropagator(
        //     new TextMapPropagator[] {
        //         new TraceContextPropagator(),
        //         new BaggagePropagator()
        //     }
        // );
        // var parentContext = propagator.Extract(
        //     default,
        //     message.Attributes,
        //     (carrier, key) =>
        //         carrier.TryGetValue(key, out var value)
        //             ? new[] { value }
        //             : Array.Empty<string>());
        //
        // Baggage.Current = parentContext.Baggage;
        //
        // var activitySource = new ActivitySource("myscan-input-api");
        // using var activity = activitySource.StartActivity(
        //     "pubsub.receive",
        //     ActivityKind.Consumer,
        //     parentContext.ActivityContext);
        
        var traceId = Activity.Current?.TraceId.ToString() ?? "no-trace";
        logger.LogInformation("Processing Pub/Sub message with trace id: {TraceId}", traceId);
        
        return Ok();
    }
}