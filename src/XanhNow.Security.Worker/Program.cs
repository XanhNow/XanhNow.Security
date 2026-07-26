using XanhNow.Security.Worker.Composition;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddXanhNowSecurityWorker(builder.Configuration, builder.Environment);

var host = builder.Build();
host.Run();
