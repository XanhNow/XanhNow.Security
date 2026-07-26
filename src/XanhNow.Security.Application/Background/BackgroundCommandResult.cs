namespace XanhNow.Security.Application.Background;

public sealed record BackgroundCommandResult(
    string CommandName,
    int Selected,
    int Processed,
    int Succeeded,
    int Failed,
    int Retried,
    int DeadLettered,
    string Outcome,
    DateTimeOffset CompletedAt)
{
    public static BackgroundCommandResult NoWork(string commandName, DateTimeOffset completedAt) =>
        new(commandName, 0, 0, 0, 0, 0, 0, "no_work", completedAt);

    public static BackgroundCommandResult Skipped(string commandName, string reason, DateTimeOffset completedAt) =>
        new(commandName, 0, 0, 0, 0, 0, 0, $"skipped:{reason}", completedAt);
}
