namespace XanhNow.Security.Migrator;

public sealed record MigratorCommandLine(MigratorMode Mode)
{
    public static bool TryParse(string[] args, out MigratorCommandLine commandLine)
    {
        commandLine = new MigratorCommandLine(MigratorMode.Validate);

        if (args.Length == 0)
        {
            return true;
        }

        if (args.Length == 1)
        {
            return TryParseMode(args[0], out commandLine);
        }

        if (args.Length == 2 && string.Equals(args[0], "--mode", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseMode(args[1], out commandLine);
        }

        return false;
    }

    private static bool TryParseMode(string value, out MigratorCommandLine commandLine)
    {
        commandLine = new MigratorCommandLine(MigratorMode.Validate);

        if (!Enum.TryParse<MigratorMode>(value, ignoreCase: true, out var mode))
        {
            return false;
        }

        commandLine = new MigratorCommandLine(mode);
        return true;
    }
}
