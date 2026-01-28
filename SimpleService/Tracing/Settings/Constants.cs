namespace SimpleService.Tracing.Settings
{
	internal static class Constants
	{
		public static class Tags
		{
			public const string TenantId = "Tenant-Id";
			public const string ServiceName = "Service-Name";
		}

		public static class Gcp
		{
			public const string OtelEndpoint = "https://otel.googleapis.com/v1/traces";
			public const string ProjectIdResourceKey = "gcp.project_id";
		}

		public static class  TraceSettings
		{
			public const string SectionName = "TRACE_SETTINGS";
		}
	}
}
