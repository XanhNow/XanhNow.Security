using XanhNow.Security.Application.Common.Requests;

namespace XanhNow.Security.Application.Background.Commands;

public sealed record RunBackgroundJobCommand(
    string JobName,
    string WorkerInstanceId,
    Guid JobRunId,
    int BatchSize,
    DateTimeOffset StartedAt) : ICommand<BackgroundCommandResult>;
