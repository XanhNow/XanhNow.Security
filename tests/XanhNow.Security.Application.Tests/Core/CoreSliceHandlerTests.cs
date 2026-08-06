using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Outbox;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Domain.Profiles;
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
            .HandleAsync(new FinishPasskeyRegistrationCommand(userId, "ceremony-1", System.Text.Json.JsonDocument.Parse("""{"id":"credential-1"}""").RootElement.Clone(), "Phone"), CancellationToken.None);
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
            BeginBindResult = new SmartOtpBindBeginResult("bind-1", "challenge-base64", 1, Now.AddMinutes(5), "PENDING"),
            CreateChallengeResult = new SmartOtpChallengeResult("challenge-1", Now.AddMinutes(5)),
            VerifyResult = new SmartOtpVerifyResult(userId, "totp")
        };
        var profiles = new FakeSecurityProfileStore();
        var unitOfWork = new FakeUnitOfWork();

        var begin = await new BeginSmartOtpEnrollmentCommandHandler(smartOtp, new FixedClock())
            .HandleAsync(new BeginSmartOtpEnrollmentCommand(userId, "Phone", "ANDROID", "app-hash", "ECDSA_P256_SHA256", "public-key", "thumbprint"), CancellationToken.None);
        var confirm = await new ConfirmSmartOtpEnrollmentCommandHandler(smartOtp, profiles, unitOfWork, new FixedClock())
            .HandleAsync(new ConfirmSmartOtpEnrollmentCommand(userId, "bind-1", "nonce", "signature"), CancellationToken.None);
        var challenge = await new StartStepUpCommandHandler(smartOtp)
            .HandleAsync(new StartStepUpCommand(userId, "transaction", "digest", Now.AddMinutes(5)), CancellationToken.None);
        var grant = await new VerifyStepUpCommandHandler(smartOtp, new FixedClock())
            .HandleAsync(new VerifyStepUpCommand("challenge-1", "123456"), CancellationToken.None);

        Assert.True(begin.IsSuccess);
        Assert.Equal("challenge-base64", begin.Value!.ServerChallenge);
        Assert.True(confirm.Value!.IsEnabled);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.True((await profiles.FindByUserIdAsync(userId, CancellationToken.None))?.SmartOtpDeviceCount > 0);
        Assert.Equal("challenge-1", challenge.Value!.ChallengeId);
        Assert.StartsWith("step-up:", grant.Value!.StepUpGrant, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Account_security_slices_delegate_to_child_app_ports()
    {
        var userId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var authLogin = new FakeAuthLoginClient { AccountStatusResult = new AuthLoginAccountStatusResult(userId, "+849******00", "Active", Now) };
        var jwt = new FakeJwtTokenClient
        {
            Sessions =
            [
                new JwtSessionDescriptor("session-1", userId, "Active", "Phone", "Android", Now.AddDays(-1), Now, Now.AddDays(30))
            ],
            RevokeAllResult = new JwtRevokeAllResult(2, Now)
        };
        var passkey = new FakePasskeyClient
        {
            ListResult = [new PasskeyDescriptor("credential-1", "Phone passkey", false)]
        };
        var audit = new FakeAuditIntentWriter();
        var profiles = new FakeSecurityProfileStore();
        await profiles.AddAsync(SecurityProfile.Create(userId, 0, 1, true, Now), CancellationToken.None);

        var password = await new ChangePasswordCommandHandler(authLogin, audit, new FixedClock())
            .HandleAsync(new ChangePasswordCommand(userId, "old-password", "new-password", "user_requested"), CancellationToken.None);
        var phone = await new StartPhoneChangeCommandHandler(authLogin, audit, new FixedClock())
            .HandleAsync(new StartPhoneChangeCommand(userId, "0911111111", "step-up-grant", "phone_change"), CancellationToken.None);
        var profile = await new GetSecurityProfileQueryHandler(authLogin, passkey, profiles)
            .HandleAsync(new GetSecurityProfileQuery(userId), CancellationToken.None);
        var sessions = await new ListSessionsQueryHandler(jwt)
            .HandleAsync(new ListSessionsQuery(userId), CancellationToken.None);
        var logoutAll = await new LogoutAllSessionsCommandHandler(jwt, audit, new FixedClock())
            .HandleAsync(new LogoutAllSessionsCommand(userId, "lost_device", true), CancellationToken.None);
        var rename = await new RenamePasskeyCommandHandler(passkey, audit, new FixedClock())
            .HandleAsync(new RenamePasskeyCommand(userId, "credential-1", "New name"), CancellationToken.None);
        var disable = await new SetPasskeyEnabledCommandHandler(passkey, audit, new FixedClock())
            .HandleAsync(new SetPasskeyEnabledCommand(userId, "credential-1", false, "user_requested"), CancellationToken.None);

        Assert.True(password.IsSuccess);
        Assert.Equal("password.change", password.Value!.OperationType);
        Assert.True(phone.IsSuccess);
        Assert.True(profile.Value!.HasPasskey);
        Assert.True(profile.Value.HasSmartOtp);
        Assert.Single(sessions.Value!);
        Assert.Equal(2, logoutAll.Value!.RevokedCount);
        Assert.True(rename.IsSuccess);
        Assert.False(disable.Value!.IsEnabled);
        Assert.Equal("0911111111", authLogin.LastPhoneChangeStartRequest?.NewPhoneNumber);
        Assert.Equal("credential-1", passkey.LastRenameRequest?.CredentialId);
        Assert.False(passkey.LastStateChangeRequest?.Enabled);
        Assert.Contains(audit.Intents, x => x.Action == "session.logout_all" && x.Outcome == "succeeded");
    }

    [Fact]
    public async Task Delete_own_account_revokes_child_apps_disables_security_user_and_writes_outbox()
    {
        var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var authLogin = new FakeAuthLoginClient();
        var jwt = new FakeJwtTokenClient { RevokeAllResult = new JwtRevokeAllResult(3, Now) };
        var passkey = new FakePasskeyClient { RevokeAllResult = new PasskeyRevokeAllResult(2, Now) };
        var smartOtp = new FakeSmartOtpClient { RevokeAllDevicesResult = new SmartOtpRevokeAllDevicesResult(1, Now) };
        var users = new FakeSecurityUserRepository();
        var outbox = new FakeOutboxIntentWriter();
        var unitOfWork = new FakeUnitOfWork();
        var audit = new FakeAuditIntentWriter();
        var handler = new DeleteOwnAccountCommandHandler(authLogin, jwt, passkey, smartOtp, users, outbox, new FakeIdGenerator(), unitOfWork, audit, new FixedClock());

        var result = await handler.HandleAsync(new DeleteOwnAccountCommand(userId, "idem-1", "corr-1", "step-up"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, authLogin.LastAccountStateChangeRequest?.UserId);
        Assert.Equal("Disabled", authLogin.LastAccountStateChangeRequest?.TargetState);
        Assert.Equal(userId, jwt.LastRevokeAllRequest?.UserId);
        Assert.True(jwt.LastRevokeAllRequest?.IncludeCurrentSession);
        Assert.Equal(userId, passkey.LastRevokeAllRequest?.UserId);
        Assert.Equal(userId, smartOtp.LastRevokeAllDevicesRequest?.UserId);

        var user = await users.FindByIdAsync(userId, CancellationToken.None);
        Assert.Equal(UserSecurityStatus.Disabled, user!.Status);
        Assert.Single(outbox.Intents);
        Assert.Equal("ACCOUNT_DELETED", outbox.Intents[0].EventType);
        Assert.Contains(audit.Intents, x => x.Action == "account.delete_self" && x.Outcome == "succeeded");
        Assert.Equal(1, unitOfWork.CommitCount);
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

    private sealed class FakeSecurityProfileStore : ISecurityProfileReader, ISecurityProfileWriter
    {
        private readonly Dictionary<Guid, SecurityProfile> _profiles = [];

        public ValueTask<SecurityProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_profiles.GetValueOrDefault(userId));

        public ValueTask AddAsync(SecurityProfile profile, CancellationToken cancellationToken)
        {
            _profiles[profile.Id] = profile;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeIdGenerator : IIdGenerator
    {
        public Guid NewId() => Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    private sealed class FakeOutboxIntentWriter : IOutboxIntentWriter
    {
        public List<OutboxIntent> Intents { get; } = [];

        public ValueTask AppendAsync(OutboxIntent intent, CancellationToken cancellationToken)
        {
            Intents.Add(intent);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeAuthLoginClient : IAuthLoginClient
    {
        public AuthLoginRegisterResult RegisterResult { get; init; } = new(Guid.NewGuid());
        public AuthLoginPasswordResult PasswordResult { get; init; } = new(Guid.NewGuid(), "pwd");
        public AuthLoginAccountStatusResult AccountStatusResult { get; init; } = new(Guid.NewGuid(), "+849******00", "Active", Now);
        public AuthLoginRegisterRequest? LastRegisterRequest { get; private set; }
        public AuthLoginPhoneChangeStartRequest? LastPhoneChangeStartRequest { get; private set; }
        public AuthLoginAccountStateChangeRequest? LastAccountStateChangeRequest { get; private set; }

        public ValueTask<ChildCallResult<AuthLoginRegisterResult>> RegisterAsync(AuthLoginRegisterRequest request, CancellationToken cancellationToken)
        {
            LastRegisterRequest = request;
            return ValueTask.FromResult(ChildCallResult<AuthLoginRegisterResult>.Success(RegisterResult));
        }

        public ValueTask<ChildCallResult<AuthLoginPasswordResult>> LoginWithPasswordAsync(AuthLoginPasswordRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginPasswordResult>.Success(PasswordResult));

        public ValueTask<ChildCallResult<AuthLoginOperationResult>> ChangePasswordAsync(AuthLoginChangePasswordRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginOperationResult>.Success(new AuthLoginOperationResult(Guid.NewGuid(), "password.change", "Accepted", "auth-login")));

        public ValueTask<ChildCallResult<AuthLoginOperationResult>> StartPasswordResetAsync(AuthLoginPasswordResetStartRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginOperationResult>.Success(new AuthLoginOperationResult(Guid.NewGuid(), "password.reset", "Accepted", "auth-login")));

        public ValueTask<ChildCallResult<AuthLoginOperationResult>> CompletePasswordResetAsync(AuthLoginPasswordResetCompleteRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginOperationResult>.Success(new AuthLoginOperationResult(Guid.NewGuid(), "password.reset.complete", "Accepted", "auth-login")));

        public ValueTask<ChildCallResult<AuthLoginAccountStateChangeResult>> ForcePasswordChangeAsync(AuthLoginForcePasswordChangeRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginAccountStateChangeResult>.Success(new AuthLoginAccountStateChangeResult(request.UserId, "Active", Now)));

        public ValueTask<ChildCallResult<AuthLoginOperationResult>> StartPhoneChangeAsync(AuthLoginPhoneChangeStartRequest request, CancellationToken cancellationToken)
        {
            LastPhoneChangeStartRequest = request;
            return ValueTask.FromResult(ChildCallResult<AuthLoginOperationResult>.Success(new AuthLoginOperationResult(Guid.NewGuid(), "phone.change", "Accepted", "auth-login")));
        }

        public ValueTask<ChildCallResult<AuthLoginOperationResult>> ConfirmPhoneChangeAsync(AuthLoginPhoneChangeConfirmRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginOperationResult>.Success(new AuthLoginOperationResult(request.OperationId, "phone.change.confirm", "Accepted", "auth-login")));

        public ValueTask<ChildCallResult<AuthLoginOperationResult>> CancelPhoneChangeAsync(AuthLoginPhoneChangeCancelRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginOperationResult>.Success(new AuthLoginOperationResult(request.OperationId, "phone.change.cancel", "Accepted", "auth-login")));

        public ValueTask<ChildCallResult<AuthLoginAccountStatusResult>> GetAccountStatusAsync(Guid userId, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<AuthLoginAccountStatusResult>.Success(AccountStatusResult with { UserId = userId }));

        public ValueTask<ChildCallResult<AuthLoginAccountStateChangeResult>> ChangeAccountStateAsync(AuthLoginAccountStateChangeRequest request, CancellationToken cancellationToken)
        {
            LastAccountStateChangeRequest = request;
            return ValueTask.FromResult(ChildCallResult<AuthLoginAccountStateChangeResult>.Success(new AuthLoginAccountStateChangeResult(request.UserId, request.TargetState, Now)));
        }
    }

    private sealed class FakeJwtTokenClient : IJwtTokenClient
    {
        public JwtIssueResult IssueResult { get; init; } = new("access", "refresh", Now.AddMinutes(15));
        public IReadOnlyCollection<JwtSessionDescriptor> Sessions { get; init; } = [];
        public JwtRevokeAllResult RevokeAllResult { get; init; } = new(0, Now);
        public JwtIssueRequest? LastIssueRequest { get; private set; }
        public JwtRevokeAllRequest? LastRevokeAllRequest { get; private set; }

        public ValueTask<ChildCallResult<JwtIssueResult>> IssueAsync(JwtIssueRequest request, CancellationToken cancellationToken)
        {
            LastIssueRequest = request;
            return ValueTask.FromResult(ChildCallResult<JwtIssueResult>.Success(IssueResult));
        }

        public ValueTask<ChildCallResult<JwtIssueResult>> RefreshAsync(JwtRefreshRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<JwtIssueResult>.Success(IssueResult));

        public ValueTask<ChildCallResult<JwtValidateResult>> ValidateAsync(string accessToken, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<JwtValidateResult>.Success(
                new JwtValidateResult(true, Guid.NewGuid(), Array.Empty<string>(), Array.Empty<string>(), "fake-session")));

        public ValueTask<ChildCallResult<bool>> RevokeSessionAsync(JwtRevokeRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<bool>.Success(true));

        public ValueTask<ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>.Success(Sessions));

        public ValueTask<ChildCallResult<JwtRevokeAllResult>> RevokeAllSessionsAsync(JwtRevokeAllRequest request, CancellationToken cancellationToken)
        {
            LastRevokeAllRequest = request;
            return ValueTask.FromResult(ChildCallResult<JwtRevokeAllResult>.Success(RevokeAllResult));
        }
    }

    private sealed class FakePasskeyClient : IPasskeyClient
    {
        public PasskeyBeginResult BeginResult { get; init; } = new("ceremony", "{}");
        public PasskeyFinishResult FinishResult { get; init; } = new(Guid.NewGuid(), "credential", "passkey");
        public IReadOnlyCollection<PasskeyDescriptor> ListResult { get; init; } = [];
        public PasskeyRevokeAllResult RevokeAllResult { get; init; } = new(0, Now);
        public PasskeyBeginRequest? LastBeginRequest { get; private set; }
        public string? LastRevokeCredentialId { get; private set; }
        public PasskeyRenameRequest? LastRenameRequest { get; private set; }
        public PasskeyStateChangeRequest? LastStateChangeRequest { get; private set; }
        public PasskeyRevokeAllRequest? LastRevokeAllRequest { get; private set; }

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

        public ValueTask<ChildCallResult<PasskeyRevokeAllResult>> RevokeAllAsync(PasskeyRevokeAllRequest request, CancellationToken cancellationToken)
        {
            LastRevokeAllRequest = request;
            return ValueTask.FromResult(ChildCallResult<PasskeyRevokeAllResult>.Success(RevokeAllResult));
        }
        public ValueTask<ChildCallResult<bool>> RenameAsync(PasskeyRenameRequest request, CancellationToken cancellationToken)
        {
            LastRenameRequest = request;
            return ValueTask.FromResult(ChildCallResult<bool>.Success(true));
        }

        public ValueTask<ChildCallResult<bool>> SetEnabledAsync(PasskeyStateChangeRequest request, CancellationToken cancellationToken)
        {
            LastStateChangeRequest = request;
            return ValueTask.FromResult(ChildCallResult<bool>.Success(true));
        }
    }

    private sealed class FakeSmartOtpClient : ISmartOtpClient
    {
        public SmartOtpBindBeginResult BeginBindResult { get; init; } = new("bind", "challenge-base64", 1, Now.AddMinutes(5), "PENDING");
        public SmartOtpChallengeResult CreateChallengeResult { get; init; } = new("challenge", Now.AddMinutes(5));
        public SmartOtpVerifyResult VerifyResult { get; init; } = new(Guid.NewGuid(), "totp");
        public SmartOtpRevokeAllDevicesResult RevokeAllDevicesResult { get; init; } = new(0, Now);
        public SmartOtpRevokeAllDevicesRequest? LastRevokeAllDevicesRequest { get; private set; }

        public ValueTask<ChildCallResult<SmartOtpBindBeginResult>> BeginBindAsync(SmartOtpBindBeginRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpBindBeginResult>.Success(BeginBindResult));

        public ValueTask<ChildCallResult<SmartOtpBindFinishResult>> FinishBindAsync(SmartOtpBindFinishRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpBindFinishResult>.Success(new SmartOtpBindFinishResult("device-1", "device-key-1", "ACTIVE", Now)));

        public ValueTask<ChildCallResult<SmartOtpChallengeResult>> CreateChallengeAsync(SmartOtpChallengeRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpChallengeResult>.Success(CreateChallengeResult));

        public ValueTask<ChildCallResult<SmartOtpVerifyResult>> VerifyAsync(SmartOtpVerifyRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpVerifyResult>.Success(VerifyResult));

        public ValueTask<ChildCallResult<SmartOtpRevokeAllDevicesResult>> RevokeAllDevicesAsync(SmartOtpRevokeAllDevicesRequest request, CancellationToken cancellationToken)
        {
            LastRevokeAllDevicesRequest = request;
            return ValueTask.FromResult(ChildCallResult<SmartOtpRevokeAllDevicesResult>.Success(RevokeAllDevicesResult));
        }
    }
}


