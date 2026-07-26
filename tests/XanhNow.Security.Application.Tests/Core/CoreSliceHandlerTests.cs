using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Domain.Users;

namespace XanhNow.Security.Application.Tests.Core;

public sealed class CoreSliceHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Register_calls_auth_login_and_creates_security_user_projection()
    {
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var authLogin = new FakeAuthLoginClient { RegisterResult = new AuthLoginRegisterResult(userId) };
        var users = new FakeSecurityUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var audit = new FakeAuditIntentWriter();
        var handler = new RegisterCommandHandler(authLogin, users, unitOfWork, audit, new FixedClock());

        var result = await handler.HandleAsync(new RegisterCommand("0900000000", "P@ssw0rd!", new DeviceContext("device-1", "Phone", "Android", null, null)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value!.UserId);
        Assert.Equal("0900000000", authLogin.LastRegisterRequest?.PhoneNumber);
        Assert.True(users.Contains(userId));
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Contains(audit.Intents, x => x.Action == "auth.register" && x.Outcome == "succeeded");
    }

    [Fact]
    public async Task Password_login_calls_auth_login_then_jwt_issue()
    {
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var authLogin = new FakeAuthLoginClient { PasswordResult = new AuthLoginPasswordResult(userId, "pwd") };
        var jwt = new FakeJwtTokenClient { IssueResult = new JwtIssueResult("access", "refresh-ref", Now.AddMinutes(15)) };
        var handler = new PasswordLoginCommandHandler(authLogin, jwt, new FakeAuditIntentWriter(), new FixedClock());

        var result = await handler.HandleAsync(new PasswordLoginCommand("0900000001", "P@ssw0rd!", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value!.State);
        Assert.Equal(userId, jwt.LastIssueRequest?.UserId);
        Assert.Equal("access", result.Value.Tokens?.AccessToken);
        Assert.Equal("refresh-ref", result.Value.Tokens?.RefreshToken);
    }

    [Fact]
    public async Task Passkey_slices_delegate_to_passkey_child_app()
    {
        var userId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var passkey = new FakePasskeyClient
        {
            BeginResult = new PasskeyBeginResult("ceremony-1", """{"challenge":"abc"}"""),
            FinishResult = new PasskeyFinishResult(userId, "credential-1", "passkey"),
            ListResult = [new PasskeyDescriptor("credential-1", "Phone passkey", false)]
        };

        var begin = await new BeginPasskeyRegistrationCommandHandler(passkey, new FixedClock())
            .HandleAsync(new BeginPasskeyRegistrationCommand(userId, "Phone passkey"), CancellationToken.None);
        var finish = await new FinishPasskeyRegistrationCommandHandler(passkey, new FixedClock())
            .HandleAsync(new FinishPasskeyRegistrationCommand("ceremony-1", System.Text.Json.JsonDocument.Parse("""{"id":"credential-1"}""").RootElement.Clone(), "Phone"), CancellationToken.None);
        var list = await new ListPasskeysQueryHandler(passkey, new FixedClock())
            .HandleAsync(new ListPasskeysQuery(userId), CancellationToken.None);
        var revoke = await new RevokePasskeyCommandHandler(passkey, new FixedClock())
            .HandleAsync(new RevokePasskeyCommand(userId, "credential-1", "user_requested"), CancellationToken.None);

        Assert.True(begin.IsSuccess);
        Assert.True(finish.IsSuccess);
        Assert.Single(list.Value!);
        Assert.True(revoke.IsSuccess);
        Assert.Equal("registration", passkey.LastBeginRequest?.Purpose);
        Assert.Equal("credential-1", passkey.LastRevokeCredentialId);
    }

    [Fact]
    public async Task Smart_otp_slices_delegate_to_smart_otp_child_app()
    {
        var userId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var smartOtp = new FakeSmartOtpClient
        {
            BeginBindResult = new SmartOtpBindBeginResult("bind-1", "otpauth://totp/xanhnow"),
            CreateChallengeResult = new SmartOtpChallengeResult("challenge-1", Now.AddMinutes(5)),
            VerifyResult = new SmartOtpVerifyResult(userId, "totp")
        };

        var begin = await new BeginSmartOtpEnrollmentCommandHandler(smartOtp, new FixedClock())
            .HandleAsync(new BeginSmartOtpEnrollmentCommand(userId, "Phone"), CancellationToken.None);
        var confirm = await new ConfirmSmartOtpEnrollmentCommandHandler(smartOtp, new FixedClock())
            .HandleAsync(new ConfirmSmartOtpEnrollmentCommand("bind-1", "123456"), CancellationToken.None);
        var challenge = await new StartStepUpCommandHandler(smartOtp)
            .HandleAsync(new StartStepUpCommand(userId, "transaction", "digest", Now.AddMinutes(5)), CancellationToken.None);
        var grant = await new VerifyStepUpCommandHandler(smartOtp, new FixedClock())
            .HandleAsync(new VerifyStepUpCommand("challenge-1", "123456"), CancellationToken.None);

        Assert.True(begin.IsSuccess);
        Assert.Equal("[REDACTED]", begin.Value!.ManualEntryKey);
        Assert.True(confirm.Value!.IsEnabled);
        Assert.Equal("challenge-1", challenge.Value!.ChallengeId);
        Assert.StartsWith("step-up:", grant.Value!.StepUpGrant, StringComparison.Ordinal);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeAuditIntentWriter : IAuditIntentWriter
    {
        public List<AuditIntent> Intents { get; } = [];

        public ValueTask AppendAsync(AuditIntent intent, CancellationToken cancellationToken)
        {
            Intents.Add(intent);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : ILocalUnitOfWork
    {
        public int CommitCount { get; private set; }

        public ValueTask CommitAsync(CancellationToken cancellationToken)
        {
            CommitCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeSecurityUserRepository : ISecurityUserRepository
    {
        private readonly Dictionary<Guid, SecurityUser> _users = [];

        public bool Contains(Guid userId) => _users.ContainsKey(userId);

        public ValueTask<SecurityUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_users.GetValueOrDefault(userId));

        public ValueTask AddAsync(SecurityUser user, CancellationToken cancellationToken)
        {
            _users[user.Id] = user;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeAuthLoginClient : IAuthLoginClient
    {
        public AuthLoginRegisterResult RegisterResult { get; init; } = new(Guid.NewGuid());
        public AuthLoginPasswordResult PasswordResult { get; init; } = new(Guid.NewGuid(), "pwd");
        public AuthLoginRegisterRequest? LastRegisterRequest { get; private set; }

        public ValueTask<ChildCallResult<AuthLoginRegisterResult>> RegisterAsync(AuthLoginRegisterRequest request, CancellationToken cancellationToken)
        {
            LastRegisterRequest = request;
            return ValueTask.FromResult(ChildCallResult<AuthLoginRegisterResult>.Success(RegisterResult));
        }

        public ValueTask<ChildCallResult<AuthLoginPasswordResult>> LoginWithPasswordAsync(AuthLoginPasswordRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginPasswordResult>.Success(PasswordResult));
    }

    private sealed class FakeJwtTokenClient : IJwtTokenClient
    {
        public JwtIssueResult IssueResult { get; init; } = new("access", "refresh", Now.AddMinutes(15));
        public JwtIssueRequest? LastIssueRequest { get; private set; }

        public ValueTask<ChildCallResult<JwtIssueResult>> IssueAsync(JwtIssueRequest request, CancellationToken cancellationToken)
        {
            LastIssueRequest = request;
            return ValueTask.FromResult(ChildCallResult<JwtIssueResult>.Success(IssueResult));
        }

        public ValueTask<ChildCallResult<JwtIssueResult>> RefreshAsync(JwtRefreshRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<JwtIssueResult>.Success(IssueResult));

        public ValueTask<ChildCallResult<bool>> RevokeSessionAsync(JwtRevokeRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<bool>.Success(true));
    }

    private sealed class FakePasskeyClient : IPasskeyClient
    {
        public PasskeyBeginResult BeginResult { get; init; } = new("ceremony", "{}");
        public PasskeyFinishResult FinishResult { get; init; } = new(Guid.NewGuid(), "credential", "passkey");
        public IReadOnlyCollection<PasskeyDescriptor> ListResult { get; init; } = [];
        public PasskeyBeginRequest? LastBeginRequest { get; private set; }
        public string? LastRevokeCredentialId { get; private set; }

        public ValueTask<ChildCallResult<PasskeyBeginResult>> BeginAsync(PasskeyBeginRequest request, CancellationToken cancellationToken)
        {
            LastBeginRequest = request;
            return ValueTask.FromResult(ChildCallResult<PasskeyBeginResult>.Success(BeginResult));
        }

        public ValueTask<ChildCallResult<PasskeyFinishResult>> FinishAsync(PasskeyFinishRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<PasskeyFinishResult>.Success(FinishResult));

        public ValueTask<ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>> ListAsync(Guid userId, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>.Success(ListResult));

        public ValueTask<ChildCallResult<bool>> RevokeAsync(Guid userId, string credentialId, CancellationToken cancellationToken)
        {
            LastRevokeCredentialId = credentialId;
            return ValueTask.FromResult(ChildCallResult<bool>.Success(true));
        }
    }

    private sealed class FakeSmartOtpClient : ISmartOtpClient
    {
        public SmartOtpBindBeginResult BeginBindResult { get; init; } = new("bind", "otpauth://totp/xanhnow");
        public SmartOtpChallengeResult CreateChallengeResult { get; init; } = new("challenge", Now.AddMinutes(5));
        public SmartOtpVerifyResult VerifyResult { get; init; } = new(Guid.NewGuid(), "totp");

        public ValueTask<ChildCallResult<SmartOtpBindBeginResult>> BeginBindAsync(SmartOtpBindBeginRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpBindBeginResult>.Success(BeginBindResult));

        public ValueTask<ChildCallResult<bool>> FinishBindAsync(SmartOtpBindFinishRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<bool>.Success(true));

        public ValueTask<ChildCallResult<SmartOtpChallengeResult>> CreateChallengeAsync(SmartOtpChallengeRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpChallengeResult>.Success(CreateChallengeResult));

        public ValueTask<ChildCallResult<SmartOtpVerifyResult>> VerifyAsync(SmartOtpVerifyRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpVerifyResult>.Success(VerifyResult));
    }
}
