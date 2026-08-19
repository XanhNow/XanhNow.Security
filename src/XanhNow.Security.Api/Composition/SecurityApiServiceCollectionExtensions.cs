using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using XanhNow.Security.Api.Health;
using XanhNow.Security.Api.Diagnostics;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Api.Options;
using XanhNow.Security.Api.Security;
using XanhNow.Security.Application.Common.Behaviors;
using XanhNow.Security.Application.Abstractions.Authorization;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Application.Abstractions.Context;
using XanhNow.Security.Infrastructure.Integration;
using XanhNow.Security.Infrastructure.Persistence;

namespace XanhNow.Security.Api.Composition;

public static class SecurityApiServiceCollectionExtensions
{
    public static IServiceCollection AddXanhNowSecurityApi(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOptions<SecurityApiOptions>()
            .Bind(configuration.GetSection(SecurityApiOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecurityApiOptions>, SecurityApiOptionsValidator>();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var proxy in configuration.GetSection("SecurityApi:KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                }
            }
        });

        services.AddHttpContextAccessor();
        services.AddSingleton<HttpCurrentCaller>();
        services.AddSingleton<ICallerContextAccessor>(sp => sp.GetRequiredService<HttpCurrentCaller>());
        services.AddSingleton<ICorrelationContextAccessor>(sp => sp.GetRequiredService<HttpCurrentCaller>());
        services.AddScoped<SecurityDependencyHealthService>();
        services.AddSingleton<OpenApiInventoryService>();
        services.AddSingleton<IAuthorizationService, CallerPermissionAuthorizationService>();
        services.AddSingleton<IApplicationExceptionReporter, LoggingApplicationExceptionReporter>();
        services.AddCoreVerticalSlices();

        services.AddSecurityPersistence(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("SecurityDb") ?? configuration["SecurityPersistence:ConnectionString"];
            options.EnableDetailedErrors = environment.IsDevelopment();
            options.EnableSensitiveDataLogging = false;
        });

        services.AddSecurityIntegration(options => configuration.GetSection("SecurityIntegration").Bind(options));

        services.AddAuthentication(SecurityAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, SecurityAuthenticationHandler>(SecurityAuthenticationHandler.SchemeName, _ => { });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(SecurityPolicyNames.User, policy => policy.RequireAuthenticatedUser().RequireClaim("caller_type", "user"));
            options.AddPolicy(SecurityPolicyNames.Service, policy => policy.RequireAuthenticatedUser().RequireClaim("caller_type", "service"));
            options.AddPolicy(SecurityPolicyNames.UserOrService, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(SecurityPolicyNames.Internal, policy => policy.RequireAuthenticatedUser().RequireClaim("caller_type", "service"));
        });

        services.AddCors(options =>
        {
            options.AddPolicy("security-cors", policy =>
            {
                var apiOptions = configuration.GetSection(SecurityApiOptions.SectionName).Get<SecurityApiOptions>() ?? new SecurityApiOptions();
                if (apiOptions.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(apiOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod();
                }
            });
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var response = ApiErrorFactory.Create(context.HttpContext, "SECURITY_RATE_LIMITED", "Request rate limit exceeded.");
                await System.Text.Json.JsonSerializer.SerializeAsync(context.HttpContext.Response.Body, response, ApiJson.SerializerOptions, cancellationToken);
            };
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var apiOptions = httpContext.RequestServices.GetRequiredService<IOptions<SecurityApiOptions>>().Value;
                var callerType = httpContext.User.FindFirst("caller_type")?.Value;
                var limit = callerType switch
                {
                    "service" => apiOptions.ServiceRequestsPerMinute,
                    "user" => apiOptions.UserRequestsPerMinute,
                    _ => apiOptions.AnonymousRequestsPerMinute
                };
                var key = callerType is null ? $"anonymous:{httpContext.Connection.RemoteIpAddress}" : $"{callerType}:{httpContext.User.Identity?.Name}";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        services.AddControllers(options =>
            {
                options.Conventions.Add(new EndpointMaturityGuardConvention());
                options.Filters.Add(new ProducesAttribute("application/json"));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = ApiJson.SerializerOptions.PropertyNamingPolicy;
                foreach (var converter in ApiJson.SerializerOptions.Converters)
                {
                    options.JsonSerializerOptions.Converters.Add(converter);
                }
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var details = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new XanhNow.Security.Contracts.Common.Errors.ApiErrorDetail("SECURITY_INVALID_FIELD", "Invalid request field.", x.Key))
                        .ToArray();
                    return new BadRequestObjectResult(ApiErrorFactory.Create(context.HttpContext, "SECURITY_VALIDATION_FAILED", "Request validation failed.", details));
                };
            });

        return services;
    }

    private static IServiceCollection AddCoreVerticalSlices(this IServiceCollection services)
    {
        services.AddScoped(typeof(ApplicationExecutor<,>));
        services.AddScoped(typeof(IApplicationBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddScoped(typeof(IApplicationBehavior<,>), typeof(CallerAuthenticationBehavior<,>));
        services.AddScoped(typeof(IApplicationBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IApplicationBehavior<,>), typeof(PolicyBehavior<,>));
        services.AddScoped(typeof(IApplicationBehavior<,>), typeof(RateLimitBehavior<,>));
        services.AddScoped(typeof(IApplicationBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IApplicationBehavior<,>), typeof(IdempotencyBehavior<,>));
        services.AddScoped(typeof(IApplicationBehavior<,>), typeof(RequestAuditBehavior<,>));
        services.AddScoped<IRequestHandler<RegisterCommand, RegisterResult>, RegisterCommandHandler>();
        services.AddScoped<IRequestHandler<PasswordLoginCommand, PasswordLoginResult>, PasswordLoginCommandHandler>();
        services.AddScoped<IRequestHandler<RefreshSessionCommand, TokenPairResult>, RefreshSessionCommandHandler>();
        services.AddScoped<IRequestHandler<LogoutSessionCommand, LogoutSessionResult>, LogoutSessionCommandHandler>();
        services.AddScoped<IRequestHandler<BeginPasskeyRegistrationCommand, BeginPasskeyRegistrationResult>, BeginPasskeyRegistrationCommandHandler>();
        services.AddScoped<IRequestHandler<FinishPasskeyRegistrationCommand, PasskeyStateResult>, FinishPasskeyRegistrationCommandHandler>();
        services.AddScoped<IRequestHandler<ListPasskeysQuery, IReadOnlyCollection<PasskeySummaryResult>>, ListPasskeysQueryHandler>();
        services.AddScoped<IRequestHandler<RevokePasskeyCommand, PasskeyStateResult>, RevokePasskeyCommandHandler>();
        services.AddScoped<IRequestHandler<BeginPasskeyLoginCommand, BeginPasskeyLoginResult>, BeginPasskeyLoginCommandHandler>();
        services.AddScoped<IRequestHandler<FinishPasskeyLoginCommand, PasswordLoginResult>, FinishPasskeyLoginCommandHandler>();
        services.AddScoped<IRequestHandler<BeginRegistrationPasskeyCommand, BeginRegistrationPasskeyResult>, BeginRegistrationPasskeyCommandHandler>();
        services.AddScoped<IRequestHandler<FinishRegistrationPasskeyCommand, FinishRegistrationPasskeyResult>, FinishRegistrationPasskeyCommandHandler>();
        services.AddScoped<IRequestHandler<BeginSmartOtpEnrollmentCommand, BeginSmartOtpEnrollmentResult>, BeginSmartOtpEnrollmentCommandHandler>();
        services.AddScoped<IRequestHandler<ConfirmSmartOtpEnrollmentCommand, SmartOtpDeviceStateResult>, ConfirmSmartOtpEnrollmentCommandHandler>();
        services.AddScoped<IRequestHandler<StartStepUpCommand, StepUpChallengeResult>, StartStepUpCommandHandler>();
        services.AddScoped<IRequestHandler<RevealStepUpCommand, StepUpRevealResult>, RevealStepUpCommandHandler>();
        services.AddScoped<IRequestHandler<VerifyStepUpCommand, StepUpGrantResult>, VerifyStepUpCommandHandler>();
        services.AddScoped<IRequestHandler<ChangePasswordCommand, AccountSecurityOperationResult>, ChangePasswordCommandHandler>();
        services.AddScoped<IRequestHandler<StartPasswordResetCommand, AccountSecurityOperationResult>, StartPasswordResetCommandHandler>();
        services.AddScoped<IRequestHandler<CompletePasswordResetCommand, AccountSecurityOperationResult>, CompletePasswordResetCommandHandler>();
        services.AddScoped<IRequestHandler<ForcePasswordChangeCommand, AccountStateResult>, ForcePasswordChangeCommandHandler>();
        services.AddScoped<IRequestHandler<StartPhoneChangeCommand, AccountSecurityOperationResult>, StartPhoneChangeCommandHandler>();
        services.AddScoped<IRequestHandler<ConfirmPhoneChangeCommand, AccountSecurityOperationResult>, ConfirmPhoneChangeCommandHandler>();
        services.AddScoped<IRequestHandler<CancelPhoneChangeCommand, AccountSecurityOperationResult>, CancelPhoneChangeCommandHandler>();
        services.AddScoped<IRequestHandler<GetSecurityProfileQuery, SecurityProfileResult>, GetSecurityProfileQueryHandler>();
        services.AddScoped<IRequestHandler<GetOperationStatusQuery, OperationStatusResult>, GetOperationStatusQueryHandler>();
        services.AddScoped<ChangeAccountStateCommandHandler>();
        services.AddScoped<IRequestHandler<ChangeAccountStateCommand, AccountStateResult>>(sp => sp.GetRequiredService<ChangeAccountStateCommandHandler>());
        services.AddScoped<IRequestHandler<DeleteOwnAccountCommand, DeleteOwnAccountResult>, DeleteOwnAccountCommandHandler>();
        services.AddScoped<IRequestHandler<ListSessionsQuery, IReadOnlyCollection<SessionSummaryResult>>, ListSessionsQueryHandler>();
        services.AddScoped<LogoutAllSessionsCommandHandler>();
        services.AddScoped<IRequestHandler<LogoutAllSessionsCommand, LogoutAllSessionsResult>>(sp => sp.GetRequiredService<LogoutAllSessionsCommandHandler>());
        services.AddScoped<IRequestHandler<RenamePasskeyCommand, PasskeyStateResult>, RenamePasskeyCommandHandler>();
        services.AddScoped<IRequestHandler<SetPasskeyEnabledCommand, PasskeyStateResult>, SetPasskeyEnabledCommandHandler>();
        services.AddScoped<IRequestHandler<EvaluateSecurityPolicyCommand, PolicyDecisionResultDto>, EvaluateSecurityPolicyCommandHandler>();
        services.AddScoped<IRequestHandler<IssueAuthGrantCommand, ProtectedGrantResult>, IssueAuthGrantCommandHandler>();
        services.AddScoped<IRequestHandler<BeginLoginMfaCommand, LoginMfaChallengeResult>, BeginLoginMfaCommandHandler>();
        services.AddScoped<IRequestHandler<CompleteLoginMfaCommand, ProtectedGrantResult>, CompleteLoginMfaCommandHandler>();
        services.AddScoped<IRequestHandler<CompletePasskeyLoginWithGrantCommand, ProtectedGrantResult>, CompletePasskeyLoginWithGrantCommandHandler>();
        services.AddScoped<IRequestHandler<IssueTransactionStepUpGrantCommand, ProtectedGrantResult>, IssueTransactionStepUpGrantCommandHandler>();
        services.AddScoped<IRequestHandler<ReportLostPhoneCommand, RecoveryWorkflowResult>, ReportLostPhoneCommandHandler>();
        services.AddScoped<IRequestHandler<StartAccountRecoveryCommand, RecoveryWorkflowResult>, StartAccountRecoveryCommandHandler>();
        services.AddScoped<IRequestHandler<CompleteAccountRecoveryCommand, RecoveryWorkflowResult>, CompleteAccountRecoveryCommandHandler>();
        services.AddScoped<IRequestHandler<ProtectAccountFromTakeoverCommand, AccountStateResult>, ProtectAccountFromTakeoverCommandHandler>();
        services.AddScoped<IRequestHandler<CompositeLockUserCommand, AccountStateResult>, CompositeLockUserCommandHandler>();
        services.AddScoped<IRequestHandler<CompositeUnlockUserCommand, AccountStateResult>, CompositeUnlockUserCommandHandler>();
        services.AddScoped<IRequestHandler<CompositeLogoutAllCommand, LogoutAllSessionsResult>, CompositeLogoutAllCommandHandler>();
        services.AddScoped<IRequestHandler<ResumeRecoveryOperationsCommand, RecoveryWorkerResult>, ResumeRecoveryOperationsCommandHandler>();
        return services;
    }
}
