namespace SimpleService.Tracing.Settings
{
	public class TracingSettings
	{
		[ConfigurationKeyName("SERVICE_NAME")]
		public string? ServiceName { get; init; }
	}
}
