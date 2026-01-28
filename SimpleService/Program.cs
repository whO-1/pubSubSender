using Serilog;
using Serilog.Enrichers.Span;

namespace SimpleService;

public static class Program
{
	public static async Task Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.Enrich.WithCorrelationIdHeader("Correlation-Id")
			.Enrich.WithSpan()
			.WriteTo.Console(new LogJsonFormatter())
			.MinimumLevel.Information()
			.CreateBootstrapLogger();

		var host = CreateHostBuilder(args).Build();
		await host.RunAsync();
	}

	public static IHostBuilder CreateHostBuilder(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false)
			.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
			.AddEnvironmentVariables()
			.Build();

		return Host.CreateDefaultBuilder(args)
			.UseSerilog()
			.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
	}
}
