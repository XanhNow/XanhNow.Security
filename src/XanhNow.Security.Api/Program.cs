using Microsoft.Extensions.Options;
using XanhNow.Security.Api.Composition;
using XanhNow.Security.Api.Middleware;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Api.Security;
using XanhNow.Security.Api.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddXanhNowSecurityApi(builder.Configuration, builder.Environment);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    var apiOptions = context.Configuration.GetSection(SecurityApiOptions.SectionName).Get<SecurityApiOptions>() ?? new SecurityApiOptions();
    options.Limits.MaxRequestBodySize = apiOptions.MaxRequestBodyBytes;
});

var app = builder.Build();
var securityOptions = app.Services.GetRequiredService<IOptions<SecurityApiOptions>>().Value;

app.UseForwardedHeaders();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (securityOptions.RequireHttps)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("security-cors");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapGet("/openapi/public-v1.json", (OpenApiInventoryService inventory) => Results.Ok(inventory.Create("public-v1")))
    .AllowAnonymous();
app.MapGet("/openapi/internal-v1.json", (OpenApiInventoryService inventory) => Results.Ok(inventory.Create("internal-v1")))
    .RequireAuthorization(SecurityPolicyNames.Internal);
app.MapControllers();

app.Run();

public partial class Program
{
}

