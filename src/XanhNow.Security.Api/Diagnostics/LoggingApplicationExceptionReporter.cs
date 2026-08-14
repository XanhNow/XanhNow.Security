using XanhNow.Security.Application.Common.Behaviors;

namespace XanhNow.Security.Api.Diagnostics;

public sealed class LoggingApplicationExceptionReporter : IApplicationExceptionReporter
{
    private readonly ILogger<LoggingApplicationExceptionReporter> _logger;

    public LoggingApplicationExceptionReporter(ILogger<LoggingApplicationExceptionReporter> logger)
    {
        _logger = logger;
    }

    public ValueTask ReportAsync(Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled application exception.");
        return ValueTask.CompletedTask;
    }
}
