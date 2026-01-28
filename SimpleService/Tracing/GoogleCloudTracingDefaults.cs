using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleService.Tracing.Settings;

namespace SimpleService.Tracing;

public static class GoogleCloudTracingDefaults
{
	public static void Configure(TracerProviderBuilder builder, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(configuration);
		
		var gcpProjectId = configuration.GetSection("SERVICE_PROJECT_ID").Value;
		if (string.IsNullOrEmpty(gcpProjectId))
		{
			using var loggerFactory = LoggerFactory.Create(_ => {});
			var logger = loggerFactory.CreateLogger(nameof(GoogleCloudTracingDefaults));
			logger.LogError("[{MethodName}] Google Cloud Tracing is missing required arguments", nameof(Configure));
			return;
		}
		
		builder.ConfigureResource(resourceBuilder =>
		{
			resourceBuilder.AddAttributes([
				new KeyValuePair<string, object>(Constants.Gcp.ProjectIdResourceKey, gcpProjectId)
			]);
		});

		builder.AddOtlpExporter(options =>
		{
			options.Endpoint = new Uri(Constants.Gcp.OtelEndpoint);
		});
	}
}
