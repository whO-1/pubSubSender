namespace SimpleService.PubSub
{
	public class PubSubSettings
	{
		[ConfigurationKeyName("SERVICE_PROJECT_ID")]
		public string? GcpProjectId { get; init; }
		[ConfigurationKeyName("EMULATOR_ENDPOINT")]
		public string? EmulatorEndpoint { get; init; }
	}
}
