using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SimpleService.Models;
using SimpleService.PubSub;
using SimpleService.Services;

namespace SimpleService.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/pubsub")]
[ApiVersion("1")]
public class PubSubController(
	IPublisherFactory publisherFactory,
	PubSubMessageStore messageStore,
	ILogger<PubSubController> logger) : ControllerBase
{
	private readonly IPublisherFactory _publisherFactory = publisherFactory;
	private readonly PubSubMessageStore _messageStore = messageStore;
	private readonly ILogger<PubSubController> _logger = logger;

	[HttpPost("publish")]
	[ProducesResponseType(StatusCodes.Status202Accepted)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Publish([FromBody] PubSubPublishRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Topic))
		{
			return BadRequest("Topic is required.");
		}

		var publisher = await _publisherFactory.GetPublisherAsync(request.Topic);
		if (publisher is null)
		{
			return StatusCode(StatusCodes.Status503ServiceUnavailable, "Publisher unavailable.");
		}

		var attributes = request.Attributes ?? new Dictionary<string, string>();
		if (Request.Headers.TryGetValue("Tenant-Id", out var tenantIdValues))
		{
			var tenantId = tenantIdValues.FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(tenantId) && !attributes.ContainsKey("Tenant-Id"))
			{
				attributes["Tenant-Id"] = tenantId;
			}
		}

		publisher.PublishMessage(string.Empty, request.Message, attributes);
		_logger.LogInformation("Published message to {Topic}", request.Topic);

		return Accepted(new { request.Topic });
	}

	[HttpPost("push")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public IActionResult ReceivePush([FromBody] PubSubPushRequest request)
	{
		var data = string.Empty;
		if (!string.IsNullOrWhiteSpace(request.Message.Data))
		{
			data = Encoding.UTF8.GetString(Convert.FromBase64String(request.Message.Data));
		}

		var attributes = request.Message.Attributes ?? new Dictionary<string, string>();
		var receivedMessage = new PubSubReceivedMessage(request.Message.MessageId, data, attributes, DateTimeOffset.UtcNow);
		_messageStore.Add(receivedMessage);

		return Ok();
	}

	[HttpGet("messages")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public IActionResult GetMessages()
	{
		return Ok(_messageStore.GetAll());
	}
}
