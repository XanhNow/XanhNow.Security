var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));

app.Run();

public partial class Program
{
}
