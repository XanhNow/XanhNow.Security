using System.Security.Cryptography;
using System.Text;
using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Grant;
using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Outbox;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Domain.Grants;
using XanhNow.Security.Domain.Profiles;
using XanhNow.Security.Domain.Users;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Tests.Core;

public sealed class CoreSliceHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);
    private static JwtIssueResult IssuedToken(string accessToken, string refreshToken, string sessionId) => new(accessToken, refreshToken, Now.AddMinutes(15), Now.AddDays(7), sessionId);

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
        Assert.Equal(UserRegistrationStatus.PendingPasskey.ToString(), result.Value.RegistrationStatus);
        Assert.Equal(userId, result.Value.Identity?.UserId);
        Assert.Equal("+84900000000", result.Value.Identity?.PhoneNumber);
        Assert.Equal("0900000000", authLogin.LastRegisterRequest?.PhoneNumber);
        Assert.True(users.Contains(userId));
        Assert.Equal("device-1", (await users.FindByIdAsync(userId, CancellationToken.None))!.RegistrationDeviceId);
        Assert.NotNull((await users.FindByIdAsync(userId, CancellationToken.None))!.RegistrationPhoneNumberHash);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Contains(audit.Intents, x => x.Action == "auth.register" && x.Outcome == "succeeded");
    }

    [Fact]
    public async Task Register_rejects_different_phone_number_for_same_app_installation()
    {
        var firstUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var secondUserId = Guid.Parse("abababab-abab-abab-abab-abababababab");
        var authLogin = new FakeAuthLoginClient { RegisterResult = new AuthLoginRegisterResult(firstUserId) };
        var users = new FakeSecurityUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var audit = new FakeAuditIntentWriter();
        var handler = new RegisterCommandHandler(authLogin, users, unitOfWork, audit, new FixedClock());

        var first = await handler.HandleAsync(
            new RegisterCommand("+84900000000", "P@ssw0rd!", new DeviceContext("device-1", "Phone", "Android", null, null)),
            CancellationToken.None);

        authLogin.RegisterResult = new AuthLoginRegisterResult(secondUserId);
        var second = await handler.HandleAsync(
            new RegisterCommand("+84900000001", "P@ssw0rd!", new DeviceContext("device-1", "Phone", "Android", null, null)),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("security.app_install_phone_conflict", second.Error?.Code);
        Assert.Equal(1, authLogin.RegisterCallCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Contains(audit.Intents, x => x.Action == "auth.register" && x.Outcome == "blocked" && x.ReasonCode == "app_install_phone_conflict");
    }

    [Fact]
    public async Task Password_login_calls_auth_login_then_jwt_issue()
    {
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var authLogin = new FakeAuthLoginClient { PasswordResult = new AuthLoginPasswordResult(userId, "pwd") };
        var jwt = new FakeJwtTokenClient { IssueResult = IssuedToken("access", "refresh-ref", "session-1") };
        var users = new FakeSecurityUserRepository();
        await users.AddAsync(SecurityUser.Create(userId, Now), CancellationToken.None);
        var handler = new PasswordLoginCommandHandler(authLogin, jwt, users, new FakeSecurityProfileStore(), new FakeUnitOfWork(), new FakeAuditIntentWriter(), new FixedClock());

        var result = await handler.HandleAsync(new PasswordLoginCommand("0900000001", "P@ssw0rd!", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value!.State);
        Assert.Equal(userId, jwt.LastIssueRequest?.UserId);
        Assert.Equal(["read:user"], jwt.LastIssueRequest?.Scopes);
        Assert.Equal("access", result.Value.Tokens?.AccessToken);
        Assert.Equal("refresh-ref", result.Value.Tokens?.RefreshToken);
        Assert.Equal(userId, result.Value.Identity?.UserId);
        Assert.Equal("+84900000001", result.Value.Identity?.PhoneNumber);
    }

    [Fact]
    public async Task Password_login_requires_smart_otp_when_profile_has_bound_device()
    {
        var userId = Guid.Parse("bcbcbcbc-bcbc-bcbc-bcbc-bcbcbcbcbcbc");
        var authLogin = new FakeAuthLoginClient { PasswordResult = new AuthLoginPasswordResult(userId, "pwd") };
        var jwt = new FakeJwtTokenClient { IssueResult = IssuedToken("access", "refresh-ref", "session-1") };
        var users = new FakeSecurityUserRepository();
        var profiles = new FakeSecurityProfileStore();
        await users.AddAsync(SecurityUser.Create(userId, Now), CancellationToken.None);
        await profiles.AddAsync(SecurityProfile.Create(userId, 1, 1, true, Now), CancellationToken.None);
        var handler = new PasswordLoginCommandHandler(authLogin, jwt, users, profiles, new FakeUnitOfWork(), new FakeAuditIntentWriter(), new FixedClock());

        var result = await handler.HandleAsync(new PasswordLoginCommand("0900000001", "P@ssw0rd!", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("MfaRequired", result.Value!.State);
        Assert.Equal("smart_otp_required", result.Value.ReasonCode);
        Assert.Equal("smart_otp", result.Value.Mfa?.Method);
        Assert.Equal(userId, result.Value.Identity?.UserId);
        Assert.Equal("+84900000001", result.Value.Identity?.PhoneNumber);
        Assert.Null(result.Value.Tokens);
        Assert.Null(jwt.LastIssueRequest);
    }
    [Fact]
    public async Task Password_login_requires_completed_registration_before_jwt_issue()
    {
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var authLogin = new FakeAuthLoginClient { PasswordResult = new AuthLoginPasswordResult(userId, "pwd") };
        var jwt = new FakeJwtTokenClient { IssueResult = IssuedToken("access", "refresh-ref", "session-1") };
        var users = new FakeSecurityUserRepository();
        await users.AddAsync(SecurityUser.CreatePendingPasskey(userId, Now), CancellationToken.None);
        var handler = new PasswordLoginCommandHandler(authLogin, jwt, users, new FakeSecurityProfileStore(), new FakeUnitOfWork(), new FakeAuditIntentWriter(), new FixedClock());

        var result = await handler.HandleAsync(new PasswordLoginCommand("0900000001", "P@ssw0rd!", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PasskeyRequired", result.Value!.State);
        Assert.Null(result.Value.Tokens);
        Assert.Null(jwt.LastIssueRequest);
    }

    [Fact]
    public async Task Password_login_recovers_missing_security_user_as_pending_passkey_and_does_not_issue_jwt()
    {
        var userId = Guid.Parse("abababab-abab-abab-abab-abababababab");
        var authLogin = new FakeAuthLoginClient { PasswordResult = new AuthLoginPasswordResult(userId, "pwd") };
        var jwt = new FakeJwtTokenClient { IssueResult = IssuedToken("access", "refresh-ref", "session-1") };
        var users = new FakeSecurityUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new PasswordLoginCommandHandler(authLogin, jwt, users, new FakeSecurityProfileStore(), unitOfWork, new FakeAuditIntentWriter(), new FixedClock());

        var result = await handler.HandleAsync(new PasswordLoginCommand("0900000001", "P@ssw0rd!", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PasskeyRequired", result.Value!.State);
        Assert.Equal("registration_passkey_required", result.Value.ReasonCode);
        Assert.Null(result.Value.Tokens);
        Assert.Null(jwt.LastIssueRequest);
        Assert.True(users.Contains(userId));
        Assert.False((await users.FindByIdAsync(userId, CancellationToken.None))!.IsRegistrationCompleted);
        Assert.Equal(1, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task Registration_flow_requires_passkey_before_password_login_can_issue_tokens()
    {
        var userId = Guid.Parse("acacacac-acac-acac-acac-acacacacacac");
        var authLogin = new FakeAuthLoginClient
        {
            RegisterResult = new AuthLoginRegisterResult(userId),
            PasswordResult = new AuthLoginPasswordResult(userId, "pwd")
        };
        var jwt = new FakeJwtTokenClient { IssueResult = IssuedToken("access", "refresh-ref", "session-1") };
        var passkey = new FakePasskeyClient
        {
            BeginResult = new PasskeyBeginResult("ceremony-1", """{"challenge":"abc"}"""),
            FinishResult = new PasskeyFinishResult(userId, "credential-1", "passkey")
        };
        var users = new FakeSecurityUserRepository();
        var profiles = new FakeSecurityProfileStore();
        var unitOfWork = new FakeUnitOfWork();
        var audit = new FakeAuditIntentWriter();
        var deviceContext = new DeviceContext("device-1", "Phone", "Android", null, null);

        var register = await new RegisterCommandHandler(authLogin, users, unitOfWork, audit, new FixedClock())
            .HandleAsync(new RegisterCommand("0900000001", "P@ssw0rd!", deviceContext), CancellationToken.None);
        var blockedLogin = await new PasswordLoginCommandHandler(authLogin, jwt, users, new FakeSecurityProfileStore(), unitOfWork, audit, new FixedClock())
            .HandleAsync(new PasswordLoginCommand("0900000001", "P@ssw0rd!", deviceContext), CancellationToken.None);
        var begin = await new BeginRegistrationPasskeyCommandHandler(passkey, users, new FixedClock())
            .HandleAsync(new BeginRegistrationPasskeyCommand(userId, "Phone passkey", deviceContext), CancellationToken.None);
        var finish = await new FinishRegistrationPasskeyCommandHandler(passkey, users, profiles, unitOfWork, new FixedClock())
            .HandleAsync(new FinishRegistrationPasskeyCommand(userId, "ceremony-1", System.Text.Json.JsonDocument.Parse("""{"id":"credential-1"}""").RootElement.Clone(), deviceContext), CancellationToken.None);
        var completedLogin = await new PasswordLoginCommandHandler(authLogin, jwt, users, new FakeSecurityProfileStore(), unitOfWork, audit, new FixedClock())
            .HandleAsync(new PasswordLoginCommand("0900000001", "P@ssw0rd!", deviceContext), CancellationToken.None);

        Assert.True(register.IsSuccess);
        Assert.Equal(UserRegistrationStatus.PendingPasskey.ToString(), register.Value!.RegistrationStatus);
        Assert.True(blockedLogin.IsSuccess);
        Assert.Equal("PasskeyRequired", blockedLogin.Value!.State);
        Assert.Null(blockedLogin.Value.Tokens);
        Assert.True(begin.IsSuccess);
        Assert.Equal("device-1", passkey.LastBeginRequest?.Device?.DeviceId);
        Assert.True(finish.IsSuccess);
        Assert.Equal(UserRegistrationStatus.Completed.ToString(), finish.Value!.RegistrationStatus);
        Assert.True(completedLogin.IsSuccess);
        Assert.Equal("Completed", completedLogin.Value!.State);
        Assert.Equal("access", completedLogin.Value.Tokens?.AccessToken);
        Assert.True((await users.FindByIdAsync(userId, CancellationToken.None))!.IsRegistrationCompleted);
        Assert.Equal(2, unitOfWork.CommitCount);
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

        var deviceContext = new DeviceContext("device-1", "Phone", "Android", null, null);
        var begin = await new BeginPasskeyRegistrationCommandHandler(passkey, new FixedClock())
            .HandleAsync(new BeginPasskeyRegistrationCommand(userId, "Phone passkey", deviceContext), CancellationToken.None);
        var users = new FakeSecurityUserRepository();
        var profiles = new FakeSecurityProfileStore();
        var unitOfWork = new FakeUnitOfWork();
        await users.AddAsync(SecurityUser.CreatePendingPasskey(userId, Now), CancellationToken.None);
        var finish = await new FinishPasskeyRegistrationCommandHandler(passkey, users, profiles, unitOfWork, new FixedClock())
            .HandleAsync(new FinishPasskeyRegistrationCommand(userId, "ceremony-1", System.Text.Json.JsonDocument.Parse("""{"id":"credential-1"}""").RootElement.Clone(), deviceContext), CancellationToken.None);
        var list = await new ListPasskeysQueryHandler(passkey, new FixedClock())
            .HandleAsync(new ListPasskeysQuery(userId), CancellationToken.None);
        var revoke = await new RevokePasskeyCommandHandler(passkey, new FixedClock())
            .HandleAsync(new RevokePasskeyCommand(userId, "credential-1", "user_requested"), CancellationToken.None);

        Assert.True(begin.IsSuccess);
        Assert.True(finish.IsSuccess);
        Assert.True((await users.FindByIdAsync(userId, CancellationToken.None))!.IsRegistrationCompleted);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Single(list.Value!);
        Assert.True(revoke.IsSuccess);
        Assert.Equal("registration", passkey.LastBeginRequest?.Purpose);
        Assert.Equal("credential-1", passkey.LastRevokeCredentialId);
    }

    [Fact]
    public async Task Passkey_login_begin_resolves_phone_to_user_id_before_calling_child_app()
    {
        var userId = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
        var passkey = new FakePasskeyClient { BeginResult = new PasskeyBeginResult("ceremony-1", """{"challenge":"abc"}""") };
        var users = new FakeSecurityUserRepository();
        await users.AddAsync(
            SecurityUser.CreatePendingPasskey(
                userId,
                Now,
                "device-1",
                "+84900000000",
                HashPhoneNumberForBinding("+84900000000")),
            CancellationToken.None);

        var result = await new BeginPasskeyLoginCommandHandler(passkey, users, new FixedClock())
            .HandleAsync(new BeginPasskeyLoginCommand("0900000000", new DeviceContext("device-1", "Phone", "Android", null, null)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("login", passkey.LastBeginRequest?.Purpose);
        Assert.Equal(userId.ToString("D"), passkey.LastBeginRequest?.LoginIdentifier);
    }

    [Fact]
    public async Task Registration_passkey_finish_requires_device_id_before_calling_child_app()
    {
        var userId = Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd");
        var passkey = new FakePasskeyClient();
        var users = new FakeSecurityUserRepository();
        await users.AddAsync(SecurityUser.CreatePendingPasskey(userId, Now), CancellationToken.None);
        var handler = new FinishRegistrationPasskeyCommandHandler(passkey, users, new FakeSecurityProfileStore(), new FakeUnitOfWork(), new FixedClock());

        var result = await handler.HandleAsync(new FinishRegistrationPasskeyCommand(
            userId,
            "ceremony-1",
            System.Text.Json.JsonDocument.Parse("""{"id":"credential-1"}""").RootElement.Clone(),
            new DeviceContext(null, "Phone", "Android", null, null)), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SecurityErrorCodes.ValidationFailed, result.Error?.Code);
        Assert.Null(passkey.LastFinishRequest);
    }

    [Fact]
    public async Task Registration_passkey_begin_requires_device_id_before_calling_child_app()
    {
        var userId = Guid.Parse("dededede-dede-dede-dede-dededededede");
        var passkey = new FakePasskeyClient();
        var users = new FakeSecurityUserRepository();
        await users.AddAsync(SecurityUser.CreatePendingPasskey(userId, Now), CancellationToken.None);
        var handler = new BeginRegistrationPasskeyCommandHandler(passkey, users, new FixedClock());

        var result = await handler.HandleAsync(new BeginRegistrationPasskeyCommand(
            userId,
            "Phone passkey",
            new DeviceContext(null, "Phone", "Android", null, null)), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SecurityErrorCodes.ValidationFailed, result.Error?.Code);
        Assert.Null(passkey.LastBeginRequest);
    }

    [Fact]
    public async Task Passkey_management_begin_requires_device_id_before_calling_child_app()
    {
        var userId = Guid.Parse("efefefef-efef-efef-efef-efefefefefef");
        var passkey = new FakePasskeyClient();
        var handler = new BeginPasskeyRegistrationCommandHandler(passkey, new FixedClock());

        var result = await handler.HandleAsync(new BeginPasskeyRegistrationCommand(
            userId,
            "Phone passkey",
            new DeviceContext(null, "Phone", "Android", null, null)), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SecurityErrorCodes.ValidationFailed, result.Error?.Code);
        Assert.Null(passkey.LastBeginRequest);
    }

    [Fact]
    public async Task Smart_otp_slices_delegate_to_smart_otp_child_app()
    {
        var userId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var smartOtp = new FakeSmartOtpClient
        {
            BeginBindResult = new SmartOtpBindBeginResult("bind-1", "challenge-base64", 1, Now, Now.AddMinutes(5), "PENDING"),
            CreateChallengeResult = new SmartOtpChallengeResult("challenge-1", "external-user", "device-1", "device-key-1", Now.AddMinutes(5), 6, 5),
            VerifyResult = new SmartOtpVerifyResult(userId, "totp")
        };
        var profiles = new FakeSecurityProfileStore();
        var unitOfWork = new FakeUnitOfWork();

        var begin = await new BeginSmartOtpEnrollmentCommandHandler(smartOtp, new FixedClock())
            .HandleAsync(new BeginSmartOtpEnrollmentCommand(userId, "Phone", "ANDROID", "app-hash", "ECDSA_P256_SHA256", "public-key", "thumbprint"), CancellationToken.None);
        var confirm = await new ConfirmSmartOtpEnrollmentCommandHandler(smartOtp, profiles, unitOfWork, new FixedClock())
            .HandleAsync(new ConfirmSmartOtpEnrollmentCommand(userId, "bind-1", "nonce", "signature"), CancellationToken.None);
        var challenge = await new StartStepUpCommandHandler(smartOtp)
            .HandleAsync(new StartStepUpCommand(userId, "device-1", "transaction", "transaction-1", "digest", Now.AddMinutes(5)), CancellationToken.None);
        var grant = await new VerifyStepUpCommandHandler(smartOtp, new FixedClock())
            .HandleAsync(new VerifyStepUpCommand(userId, "challenge-1", "device-1", "transaction", "transaction-1", "digest", "123456"), CancellationToken.None);

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
        var phoneGrants = FakeSecurityGrantRepository.WithActiveGrant(userId, "phone.change");
        var phone = await new StartPhoneChangeCommandHandler(authLogin, new FakeGrantProtector(phoneGrants.GrantId, userId), phoneGrants, new FakeUnitOfWork(), audit, new FixedClock())
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
        var deleteGrants = FakeSecurityGrantRepository.WithActiveGrant(userId, "account_self_delete");
        var handler = new DeleteOwnAccountCommandHandler(authLogin, jwt, passkey, smartOtp, new FakeGrantProtector(deleteGrants.GrantId, userId), deleteGrants, users, outbox, new FakeIdGenerator(), unitOfWork, audit, new FixedClock());

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

    private static string HashPhoneNumberForBinding(string phoneNumber)
    {
        var normalized = phoneNumber.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        if (normalized.StartsWith("84", StringComparison.Ordinal))
        {
            normalized = "+" + normalized;
        }
        else if (normalized.StartsWith("0", StringComparison.Ordinal))
        {
            normalized = "+84" + normalized[1..];
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
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

        public ValueTask<SecurityUser?> FindByRegistrationDeviceIdAsync(string registrationDeviceId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_users.Values.SingleOrDefault(x => x.RegistrationDeviceId == registrationDeviceId));

        public ValueTask<SecurityUser?> FindByRegistrationPhoneNumberHashAsync(string registrationPhoneNumberHash, CancellationToken cancellationToken)
            => ValueTask.FromResult(_users.Values.SingleOrDefault(x => x.RegistrationPhoneNumberHash == registrationPhoneNumberHash));

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


    private sealed class FakeGrantProtector : IGrantProtector
    {
        private readonly Guid _grantId;
        private readonly Guid _userId;

        public FakeGrantProtector(Guid grantId, Guid userId)
        {
            _grantId = grantId;
            _userId = userId;
        }

        public ValueTask<ProtectedGrant> ProtectAsync(Guid grantId, Guid userId, string purpose, DateTimeOffset expiresAt, CancellationToken cancellationToken)
            => ValueTask.FromResult(new ProtectedGrant("protected-grant", expiresAt));

        public ValueTask<ProtectedGrantVerification> VerifyAsync(string protectedGrant, string expectedPurpose, CancellationToken cancellationToken)
            => ValueTask.FromResult(new ProtectedGrantVerification(true, _grantId, _userId, expectedPurpose));

        public ValueTask<bool> TryMarkUsedAsync(string replayKey, DateTimeOffset expiresAt, CancellationToken cancellationToken)
            => ValueTask.FromResult(true);
    }

    private sealed class FakeSecurityGrantRepository : ISecurityGrantRepository
    {
        private readonly Dictionary<Guid, SecurityGrant> _grants = [];

        public Guid GrantId { get; private init; }

        public static FakeSecurityGrantRepository WithActiveGrant(Guid userId, string purpose)
        {
            var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var grant = SecurityGrant.Issue(
                id,
                userId,
                SecurityGrantType.StepUpGrant,
                GrantAudience.From("security"),
                GrantPurpose.From(purpose),
                Now.AddMinutes(-1),
                Now.AddMinutes(5));
            grant.Activate(Now);
            var repository = new FakeSecurityGrantRepository { GrantId = id };
            repository._grants[id] = grant;
            return repository;
        }

        public ValueTask<SecurityGrant?> FindByIdAsync(Guid grantId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_grants.GetValueOrDefault(grantId));

        public ValueTask AddAsync(SecurityGrant grant, CancellationToken cancellationToken)
        {
            _grants[grant.Id] = grant;
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
        public AuthLoginRegisterResult RegisterResult { get; set; } = new(Guid.NewGuid());
        public AuthLoginPasswordResult PasswordResult { get; init; } = new(Guid.NewGuid(), "pwd");
        public AuthLoginAccountStatusResult AccountStatusResult { get; init; } = new(Guid.NewGuid(), "+849******00", "Active", Now);
        public int RegisterCallCount { get; private set; }
        public AuthLoginRegisterRequest? LastRegisterRequest { get; private set; }
        public AuthLoginPhoneChangeStartRequest? LastPhoneChangeStartRequest { get; private set; }
        public AuthLoginAccountStateChangeRequest? LastAccountStateChangeRequest { get; private set; }

        public ValueTask<ChildCallResult<AuthLoginRegisterResult>> RegisterAsync(AuthLoginRegisterRequest request, CancellationToken cancellationToken)
        {
            RegisterCallCount++;
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
        public JwtIssueResult IssueResult { get; init; } = IssuedToken("access", "refresh", "fake-session");
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
        public PasskeyFinishRequest? LastFinishRequest { get; private set; }
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
        {
            LastFinishRequest = request;
            return ValueTask.FromResult(ChildCallResult<PasskeyFinishResult>.Success(FinishResult));
        }

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
        public SmartOtpBindBeginResult BeginBindResult { get; init; } = new("bind", "challenge-base64", 1, Now, Now.AddMinutes(5), "PENDING");
        public SmartOtpChallengeResult CreateChallengeResult { get; init; } = new("challenge", "external-user", "device-1", "device-key-1", Now.AddMinutes(5), 6, 5);
        public SmartOtpRevealResult RevealResult { get; init; } = new("challenge", "123456", Now.AddMinutes(5), 1, Now);
        public SmartOtpVerifyResult VerifyResult { get; init; } = new(Guid.NewGuid(), "totp");
        public SmartOtpRevokeAllDevicesResult RevokeAllDevicesResult { get; init; } = new(0, Now);
        public SmartOtpRevokeAllDevicesRequest? LastRevokeAllDevicesRequest { get; private set; }

        public ValueTask<ChildCallResult<SmartOtpBindBeginResult>> BeginBindAsync(SmartOtpBindBeginRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpBindBeginResult>.Success(BeginBindResult));

        public ValueTask<ChildCallResult<SmartOtpBindFinishResult>> FinishBindAsync(SmartOtpBindFinishRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpBindFinishResult>.Success(new SmartOtpBindFinishResult("device-1", "device-key-1", "ACTIVE", Now)));

        public ValueTask<ChildCallResult<SmartOtpChallengeResult>> CreateChallengeAsync(SmartOtpChallengeRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpChallengeResult>.Success(CreateChallengeResult));

        public ValueTask<ChildCallResult<SmartOtpRevealResult>> RevealAsync(SmartOtpRevealRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpRevealResult>.Success(RevealResult));

        public ValueTask<ChildCallResult<SmartOtpVerifyResult>> VerifyAsync(SmartOtpVerifyRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(ChildCallResult<SmartOtpVerifyResult>.Success(VerifyResult));

        public ValueTask<ChildCallResult<SmartOtpRevokeAllDevicesResult>> RevokeAllDevicesAsync(SmartOtpRevokeAllDevicesRequest request, CancellationToken cancellationToken)
        {
            LastRevokeAllDevicesRequest = request;
            return ValueTask.FromResult(ChildCallResult<SmartOtpRevokeAllDevicesResult>.Success(RevokeAllDevicesResult));
        }
    }
}
