using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XanhNow.Security.Migrator;
using XanhNow.Security.Migrator.Composition;

if (!MigratorCommandLine.TryParse(args, out var commandLine))
{
    Console.Error.WriteLine("Usage: XanhNow.Security.Migrator [validate|plan|apply] or --mode <validate|plan|apply>");
    return (int)MigratorExitCode.InvalidArguments;
}

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
    builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
    builder.Configuration.AddEnvironmentVariables();
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    });

    builder.Services.AddXanhNowSecurityMigrator(builder.Configuration);

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
    var exitCode = await runner.RunAsync(commandLine.Mode, CancellationToken.None).ConfigureAwait(false);
    return (int)exitCode;
}
catch (OptionsValidationException ex)
{
    Console.Error.WriteLine($"Security migrator configuration error: {string.Join("; ", ex.Failures)}");
    return (int)MigratorExitCode.ConfigurationError;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Security migrator startup failure: {ex.GetType().Name}");
    return (int)MigratorExitCode.UnexpectedFailure;
}
