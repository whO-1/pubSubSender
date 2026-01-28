using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using SimpleService.PubSub;
using SimpleService.Services;
using SimpleService.Tracing;

namespace SimpleService;

public class Startup
{
	public Startup(IConfiguration configuration, IWebHostEnvironment environment)
	{
		Configuration = configuration;
		Environment = environment;
	}

	public IConfiguration Configuration { get; }
	public IWebHostEnvironment Environment { get; }

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddControllers();
		services.AddApiVersioning(options =>
			{
				options.ReportApiVersions = true;
				options.AssumeDefaultVersionWhenUnspecified = true;
			})
			.AddApiExplorer(options =>
			{
				options.FormatGroupName = (group, version) => $"{group}-{version}";
				options.GroupNameFormat = "'v'VVV";
				options.SubstituteApiVersionInUrl = true;
			});
		
		services.AddHttpClient();
		
		//tracing
		services.AddDefaultOpenTelemetryTracing(Configuration);
		
		//pubsub
		services.Configure<PubSubSettings>(Configuration);
		services.AddMemoryCache();
		services.TryAddSingleton<IPublisherFactory, PubSubPublisherFactory>();
		//
		
		services.AddSingleton<PubSubMessageStore>();
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
	{
		app.UseSerilogRequestLogging();
		app.UseRouting();
		app.UseExceptionHandler("/error");
		app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
	}
}
