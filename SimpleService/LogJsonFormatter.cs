using Serilog.Templates;

namespace SimpleService;

public class LogJsonFormatter : ExpressionTemplate
{
    public const string TEMPLATE = "{ {time: UtcDateTime(@t), " +
                                   "severity: @l, " +
                                   "message: @m, " +
                                   "MessageTemplate: @mt, " +
                                   "exception: @x, " +
                                   "'logging.googleapis.com/spanId': SpanId, " +
                                   // https://cloud.google.com/logging/docs/reference/v2/rest/v2/LogEntry#FIELDS.trace
                                   "'logging.googleapis.com/trace': Concat('projects/', GcpProjectId, '/traces/', TraceId), " +
                                   "'logging.googleapis.com/labels': { " +
                                   "'Tenant-Id': TenantIdFromContext, " +
                                   "'Route-Template': RouteTemplate, " +
                                   "'Correlation-Id': CorrelationId, " +
                                   "'Billable': Billable " +
                                   "}, " +
                                   "..rest() }}\n";

    public LogJsonFormatter() : base(TEMPLATE)
    {
    }
}