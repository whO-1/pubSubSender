using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleService.Tracing.Settings;

namespace SimpleService.Tracing
{
	public static class TracingServiceExtensions
	{
		public static IServiceCollection AddDefaultOpenTelemetryTracing(this IServiceCollection services,
			IConfiguration configuration, Action<TracerProviderBuilder>? configure = null)
		{
			ArgumentNullException.ThrowIfNull(services);

			using var loggerFactory = LoggerFactory.Create(_ => {});
			var logger = loggerFactory.CreateLogger(nameof(TracingServiceExtensions));

			var serviceName = configuration.GetSection("SERVICE_NAME").Value;
			if (string.IsNullOrEmpty(serviceName))
			{
				logger.LogError("OpenTelemetry tracing settings are not configured.");
				return services;
			}

			services.Configure<TracingSettings>(configuration);
		
			
			services.AddOpenTelemetry()
				.ConfigureResource(resource => resource
					.AddService(serviceName: serviceName))
				.WithTracing(builder =>
				{
					builder
						.AddSource(serviceName)
						.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName))
						.AddAspNetCoreInstrumentation(options =>
						{
							options.Filter = ctx =>
							{
								var path = ctx.Request.Path;
								return !path.Value!.EndsWith("/push", StringComparison.OrdinalIgnoreCase);
							};
						})
						.AddHttpClientInstrumentation();

					configure?.Invoke(builder);
				});

			return services;
		}
	}
}
