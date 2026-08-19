using XanhNow.Security.Application.Abstractions.Audit;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Application.Abstractions.Grant;
using XanhNow.Security.Application.Abstractions.Ids;
using XanhNow.Security.Application.Abstractions.Persistence;
using XanhNow.Security.Application.Abstractions.Policy;
using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Common.ChildApps;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;
using XanhNow.Security.Domain.Grants;
using XanhNow.Security.Domain.Policies;
using XanhNow.Security.Domain.Recovery;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Core;

internal static class RecoveryPolicyStepUpMapper
{
    public static string StepUpPurpose(string purpose, string transactionId, string digest, string canonicalizationVersion)
        => $"{purpose}|tx:{transactionId}|digest:{digest}|canon:{canonicalizationVersion}";

    public static PolicyDecisionResult MapPolicyDecision(bool allowed) => allowed ? PolicyDecisionResult.Allow : PolicyDecisionResult.Deny;

    public static RecoveryWorkflowResult ToRecovery(SecurityRecoveryCase recovery, string step, DateTimeOffset updatedAtUtc)
        => new(recovery.Id, recovery.UserId, recovery.Status.ToString(), step, updatedAtUtc);
}

public sealed class EvaluateSecurityPolicyCommandHandler : IRequestHandler<EvaluateSecurityPolicyCommand, PolicyDecisionResultDto>
{
    private readonly IPolicyEvaluator _policyEvaluator;
    private readonly ISecurityPolicyDecisionWriter _decisions;
    private readonly IIdGenerator _ids;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EvaluateSecurityPolicyCommandHandler(IPolicyEvaluator policyEvaluator, ISecurityPolicyDecisionWriter decisions, IIdGenerator ids, ILocalUnitOfWork unitOfWork, IClock clock)
    {
        _policyEvaluator = policyEvaluator;
        _decisions = decisions;
        _ids = ids;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PolicyDecisionResultDto>> HandleAsync(EvaluateSecurityPolicyCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _policyEvaluator.EvaluateAsync(new PolicyContext(request.UserId, request.Purpose, request.AssuranceLevel, request.Context), cancellationToken);
        var decisionId = _ids.NewId();
        if (request.UserId.HasValue)
        {
            await _decisions.AppendAsync(SecurityPolicyDecision.Create(
                decisionId,
                request.UserId.Value,
                PolicyCode.From(request.PolicyCode),
                evaluation.PolicyVersion,
                RecoveryPolicyStepUpMapper.MapPolicyDecision(evaluation.Allowed),
                ReasonCode.From(evaluation.ReasonCode),
                _clock.UtcNow), cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        return Result<PolicyDecisionResultDto>.Success(new PolicyDecisionResultDto(
            decisionId,
            request.PolicyCode,
            evaluation.Allowed ? "Allow" : "Deny",
            evaluation.ReasonCode,
            evaluation.PolicyVersion,
            _clock.UtcNow));
    }
}

public sealed class IssueAuthGrantCommandHandler : IRequestHandler<IssueAuthGrantCommand, ProtectedGrantResult>
{
    private readonly ISecurityGrantRepository _grants;
    private readonly IGrantProtector _protector;
    private readonly IIdGenerator _ids;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public IssueAuthGrantCommandHandler(ISecurityGrantRepository grants, IGrantProtector protector, IIdGenerator ids, ILocalUnitOfWork unitOfWork, IClock clock)
    {
        _grants = grants;
        _protector = protector;
        _ids = ids;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ProtectedGrantResult>> HandleAsync(IssueAuthGrantCommand request, CancellationToken cancellationToken)
    {
        var grant = await IssueGrantAsync(_grants, _protector, _ids, _unitOfWork, _clock, request.UserId, SecurityGrantType.AuthGrant, request.Audience, request.Purpose, request.Lifetime, cancellationToken);
        return Result<ProtectedGrantResult>.Success(grant);
    }

    internal static async Task<ProtectedGrantResult> IssueGrantAsync(
        ISecurityGrantRepository grants,
        IGrantProtector protector,
        IIdGenerator ids,
        ILocalUnitOfWork unitOfWork,
        IClock clock,
        Guid userId,
        SecurityGrantType grantType,
        string audience,
        string purpose,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var expiresAt = now.Add(lifetime);
        var grantId = ids.NewId();
        var grant = SecurityGrant.Issue(grantId, userId, grantType, GrantAudience.From(audience), GrantPurpose.From(purpose), now, expiresAt);
        grant.Activate(now);
        await grants.AddAsync(grant, cancellationToken);
        var protectedGrant = await protector.ProtectAsync(grantId, userId, purpose, expiresAt, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return new ProtectedGrantResult(grantId, protectedGrant.Value, grantType.ToString(), audience, purpose, protectedGrant.ExpiresAt);
    }
}

public sealed class BeginLoginMfaCommandHandler : IRequestHandler<BeginLoginMfaCommand, LoginMfaChallengeResult>
{
    private readonly ISmartOtpClient _smartOtp;

    public BeginLoginMfaCommandHandler(ISmartOtpClient smartOtp) => _smartOtp = smartOtp;

    public async Task<Result<LoginMfaChallengeResult>> HandleAsync(BeginLoginMfaCommand request, CancellationToken cancellationToken)
    {
        var child = await _smartOtp.CreateChallengeAsync(new SmartOtpChallengeRequest(request.UserId, string.Empty, "login_mfa", request.LoginOperationId, request.TransactionDigest), cancellationToken);
        return child.IsSuccess && child.Value is not null
            ? Result<LoginMfaChallengeResult>.Success(new LoginMfaChallengeResult(request.UserId, child.Value.ChallengeId, "totp", "login_mfa", child.Value.ExpiresAt))
            : CoreSliceHandler.ChildFailure<LoginMfaChallengeResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP login MFA challenge failed.", true));
    }
}

public sealed class CompleteLoginMfaCommandHandler : IRequestHandler<CompleteLoginMfaCommand, ProtectedGrantResult>
{
    private readonly ISmartOtpClient _smartOtp;
    private readonly ISecurityGrantRepository _grants;
    private readonly IGrantProtector _protector;
    private readonly IIdGenerator _ids;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CompleteLoginMfaCommandHandler(ISmartOtpClient smartOtp, ISecurityGrantRepository grants, IGrantProtector protector, IIdGenerator ids, ILocalUnitOfWork unitOfWork, IClock clock)
    {
        _smartOtp = smartOtp;
        _grants = grants;
        _protector = protector;
        _ids = ids;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ProtectedGrantResult>> HandleAsync(CompleteLoginMfaCommand request, CancellationToken cancellationToken)
    {
        var verified = await _smartOtp.VerifyAsync(new SmartOtpVerifyRequest(request.UserId, request.ChallengeId, string.Empty, "login_mfa", string.Empty, string.Empty, new SensitiveString(request.TotpCode)), cancellationToken);
        if (verified.IsFailure || verified.Value is null || verified.Value.UserId != request.UserId)
        {
            return CoreSliceHandler.ChildFailure<ProtectedGrantResult>(verified.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP login MFA verify failed.", true));
        }

        var grant = await IssueAuthGrantCommandHandler.IssueGrantAsync(_grants, _protector, _ids, _unitOfWork, _clock, request.UserId, SecurityGrantType.AuthGrant, request.Audience, "login_mfa", TimeSpan.FromMinutes(5), cancellationToken);
        return Result<ProtectedGrantResult>.Success(grant);
    }
}

public sealed class CompletePasskeyLoginWithGrantCommandHandler : IRequestHandler<CompletePasskeyLoginWithGrantCommand, ProtectedGrantResult>
{
    private readonly IPasskeyClient _passkey;
    private readonly ISecurityGrantRepository _grants;
    private readonly IGrantProtector _protector;
    private readonly IIdGenerator _ids;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CompletePasskeyLoginWithGrantCommandHandler(IPasskeyClient passkey, ISecurityGrantRepository grants, IGrantProtector protector, IIdGenerator ids, ILocalUnitOfWork unitOfWork, IClock clock)
    {
        _passkey = passkey;
        _grants = grants;
        _protector = protector;
        _ids = ids;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ProtectedGrantResult>> HandleAsync(CompletePasskeyLoginWithGrantCommand request, CancellationToken cancellationToken)
    {
        var child = await _passkey.FinishAsync(new PasskeyFinishRequest(Guid.Empty, request.CeremonyId, request.CredentialJson, null), cancellationToken);
        if (child.IsFailure || child.Value is null)
        {
            return CoreSliceHandler.ChildFailure<ProtectedGrantResult>(child.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Passkey login finish failed.", true));
        }

        var grant = await IssueAuthGrantCommandHandler.IssueGrantAsync(_grants, _protector, _ids, _unitOfWork, _clock, child.Value.UserId, SecurityGrantType.AuthGrant, request.Audience, "passkey_login", TimeSpan.FromMinutes(5), cancellationToken);
        return Result<ProtectedGrantResult>.Success(grant);
    }
}

public sealed class IssueTransactionStepUpGrantCommandHandler : IRequestHandler<IssueTransactionStepUpGrantCommand, ProtectedGrantResult>
{
    private readonly ISmartOtpClient _smartOtp;
    private readonly ISecurityGrantRepository _grants;
    private readonly IGrantProtector _protector;
    private readonly IIdGenerator _ids;
    private readonly ILocalUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public IssueTransactionStepUpGrantCommandHandler(ISmartOtpClient smartOtp, ISecurityGrantRepository grants, IGrantProtector protector, IIdGenerator ids, ILocalUnitOfWork unitOfWork, IClock clock)
    {
        _smartOtp = smartOtp;
        _grants = grants;
        _protector = protector;
        _ids = ids;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ProtectedGrantResult>> HandleAsync(IssueTransactionStepUpGrantCommand request, CancellationToken cancellationToken)
    {
        var verified = await _smartOtp.VerifyAsync(new SmartOtpVerifyRequest(request.UserId, request.ChallengeId, string.Empty, request.Purpose, request.TransactionId, request.TransactionDigest, new SensitiveString(request.TotpCode)), cancellationToken);
        if (verified.IsFailure || verified.Value is null || verified.Value.UserId != request.UserId)
        {
            return CoreSliceHandler.ChildFailure<ProtectedGrantResult>(verified.Error ?? new ChildCallError(SecurityErrorCodes.DownstreamUnavailable, "Smart OTP transaction step-up verify failed.", true));
        }

        var boundPurpose = RecoveryPolicyStepUpMapper.StepUpPurpose(request.Purpose, request.TransactionId, request.TransactionDigest, request.CanonicalizationVersion);
        var grant = await IssueAuthGrantCommandHandler.IssueGrantAsync(_grants, _protector, _ids, _unitOfWork, _clock, request.UserId, SecurityGrantType.StepUpGrant, request.Audience, boundPurpose, TimeSpan.FromMinutes(5), cancellationToken);
        return Result<ProtectedGrantResult>.Success(grant);
    }
}

public sealed class ReportLostPhoneCommandHandler : CoreSliceHandler, IRequestHandler<ReportLostPhoneCommand, RecoveryWorkflowResult>
{
    private readonly ISecurityRecoveryCaseRepository _recoveryCases;
    private readonly IAuthLoginClient _authLogin;
    private readonly IJwtTokenClient _jwt;
    private readonly IPasskeyClient _passkey;
    private readonly IIdGenerator _ids;
    private readonly ILocalUnitOfWork _unitOfWork;

    public ReportLostPhoneCommandHandler(ISecurityRecoveryCaseRepository recoveryCases, IAuthLoginClient authLogin, IJwtTokenClient jwt, IPasskeyClient passkey, IIdGenerator ids, ILocalUnitOfWork unitOfWork, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock)
    {
        _recoveryCases = recoveryCases;
        _authLogin = authLogin;
        _jwt = jwt;
        _passkey = passkey;
        _ids = ids;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecoveryWorkflowResult>> HandleAsync(ReportLostPhoneCommand request, CancellationToken cancellationToken)
    {
        var recovery = SecurityRecoveryCase.Open(_ids.NewId(), request.UserId, ReasonCode.From(request.ReasonCode), Now);
        recovery.BeginProofVerification(Now);
        recovery.ProtectAccount(Now);
        await _authLogin.ChangeAccountStateAsync(new AuthLoginAccountStateChangeRequest(request.UserId, "Locked", request.ReasonCode, $"lost device: {request.DeviceReference}"), cancellationToken);
        recovery.RevokeSessions(Now);
        await _jwt.RevokeAllSessionsAsync(new JwtRevokeAllRequest(request.UserId, request.ReasonCode, true), cancellationToken);
        recovery.DisableAuthenticators(Now);
        var credentials = await _passkey.ListAsync(request.UserId, cancellationToken);
        if (credentials.IsSuccess && credentials.Value is not null)
        {
            foreach (var credential in credentials.Value.Where(x => !x.Revoked))
            {
                await _passkey.SetEnabledAsync(new PasskeyStateChangeRequest(request.UserId, credential.CredentialId, false, request.ReasonCode), cancellationToken);
            }
        }

        await _recoveryCases.AddAsync(recovery, cancellationToken);
        await AuditAsync(request.UserId, "recovery.lost_phone", "accepted", request.ReasonCode, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<RecoveryWorkflowResult>.Success(RecoveryPolicyStepUpMapper.ToRecovery(recovery, "disabling-authenticators", Now));
    }
}

public sealed class StartAccountRecoveryCommandHandler : CoreSliceHandler, IRequestHandler<StartAccountRecoveryCommand, RecoveryWorkflowResult>
{
    private readonly ISecurityRecoveryCaseRepository _recoveryCases;
    private readonly IIdGenerator _ids;
    private readonly ILocalUnitOfWork _unitOfWork;

    public StartAccountRecoveryCommandHandler(ISecurityRecoveryCaseRepository recoveryCases, IIdGenerator ids, ILocalUnitOfWork unitOfWork, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock)
    {
        _recoveryCases = recoveryCases;
        _ids = ids;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecoveryWorkflowResult>> HandleAsync(StartAccountRecoveryCommand request, CancellationToken cancellationToken)
    {
        var recovery = SecurityRecoveryCase.Open(_ids.NewId(), request.UserId, ReasonCode.From(request.ReasonCode), Now);
        await _recoveryCases.AddAsync(recovery, cancellationToken);
        await AuditAsync(request.UserId, "recovery.start", "accepted", request.ReasonCode, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<RecoveryWorkflowResult>.Success(RecoveryPolicyStepUpMapper.ToRecovery(recovery, "pending-proof", Now));
    }
}

public sealed class CompleteAccountRecoveryCommandHandler : CoreSliceHandler, IRequestHandler<CompleteAccountRecoveryCommand, RecoveryWorkflowResult>
{
    private readonly ISecurityRecoveryCaseRepository _recoveryCases;
    private readonly IAuthLoginClient _authLogin;
    private readonly ILocalUnitOfWork _unitOfWork;

    public CompleteAccountRecoveryCommandHandler(ISecurityRecoveryCaseRepository recoveryCases, IAuthLoginClient authLogin, ILocalUnitOfWork unitOfWork, IAuditIntentWriter audit, IClock clock)
        : base(audit, clock)
    {
        _recoveryCases = recoveryCases;
        _authLogin = authLogin;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecoveryWorkflowResult>> HandleAsync(CompleteAccountRecoveryCommand request, CancellationToken cancellationToken)
    {
        var recovery = await _recoveryCases.FindByIdAsync(request.RecoveryCaseId, cancellationToken);
        if (recovery is null || recovery.UserId != request.UserId)
        {
            return Result<RecoveryWorkflowResult>.Failure(Error.NotFound("SECURITY_RECOVERY_CASE_NOT_FOUND", "Recovery case was not found."));
        }

        while (recovery.Status != RecoveryCaseStatus.RestoringAccess)
        {
            switch (recovery.Status)
            {
                case RecoveryCaseStatus.Pending:
                    recovery.BeginProofVerification(Now);
                    break;
                case RecoveryCaseStatus.VerifyingProof:
                    recovery.ProtectAccount(Now);
                    break;
                case RecoveryCaseStatus.ProtectingAccount:
                    recovery.RevokeSessions(Now);
                    break;
                case RecoveryCaseStatus.RevokingSessions:
                    recovery.DisableAuthenticators(Now);
                    break;
                case RecoveryCaseStatus.DisablingAuthenticators:
                    recovery.RestoreAccess(Now);
                    break;
            }
        }

        await _authLogin.ChangeAccountStateAsync(new AuthLoginAccountStateChangeRequest(request.UserId, "Active", request.ReasonCode, "recovery completed"), cancellationToken);
        recovery.Complete(Now);
        await AuditAsync(request.UserId, "recovery.complete", "succeeded", request.ReasonCode, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<RecoveryWorkflowResult>.Success(RecoveryPolicyStepUpMapper.ToRecovery(recovery, "completed", Now));
    }
}

public sealed class ProtectAccountFromTakeoverCommandHandler : IRequestHandler<ProtectAccountFromTakeoverCommand, AccountStateResult>
{
    private readonly ChangeAccountStateCommandHandler _change;

    public ProtectAccountFromTakeoverCommandHandler(ChangeAccountStateCommandHandler change) => _change = change;

    public Task<Result<AccountStateResult>> HandleAsync(ProtectAccountFromTakeoverCommand request, CancellationToken cancellationToken)
        => _change.HandleAsync(new ChangeAccountStateCommand(request.UserId, AccountStateTargetState.Locked, request.ReasonCode, "account takeover protection"), cancellationToken);
}

public sealed class CompositeLockUserCommandHandler : IRequestHandler<CompositeLockUserCommand, AccountStateResult>
{
    private readonly ChangeAccountStateCommandHandler _change;

    public CompositeLockUserCommandHandler(ChangeAccountStateCommandHandler change) => _change = change;

    public Task<Result<AccountStateResult>> HandleAsync(CompositeLockUserCommand request, CancellationToken cancellationToken)
        => _change.HandleAsync(new ChangeAccountStateCommand(request.UserId, AccountStateTargetState.Locked, request.ReasonCode, request.Comment), cancellationToken);
}

public sealed class CompositeUnlockUserCommandHandler : IRequestHandler<CompositeUnlockUserCommand, AccountStateResult>
{
    private readonly ChangeAccountStateCommandHandler _change;

    public CompositeUnlockUserCommandHandler(ChangeAccountStateCommandHandler change) => _change = change;

    public Task<Result<AccountStateResult>> HandleAsync(CompositeUnlockUserCommand request, CancellationToken cancellationToken)
        => _change.HandleAsync(new ChangeAccountStateCommand(request.UserId, AccountStateTargetState.Active, request.ReasonCode, request.Comment), cancellationToken);
}

public sealed class CompositeLogoutAllCommandHandler : IRequestHandler<CompositeLogoutAllCommand, LogoutAllSessionsResult>
{
    private readonly LogoutAllSessionsCommandHandler _logoutAll;

    public CompositeLogoutAllCommandHandler(LogoutAllSessionsCommandHandler logoutAll) => _logoutAll = logoutAll;

    public Task<Result<LogoutAllSessionsResult>> HandleAsync(CompositeLogoutAllCommand request, CancellationToken cancellationToken)
        => _logoutAll.HandleAsync(new LogoutAllSessionsCommand(request.UserId, request.ReasonCode, true), cancellationToken);
}

public sealed class ResumeRecoveryOperationsCommandHandler : IRequestHandler<ResumeRecoveryOperationsCommand, RecoveryWorkerResult>
{
    private readonly IClock _clock;

    public ResumeRecoveryOperationsCommandHandler(IClock clock) => _clock = clock;

    public Task<Result<RecoveryWorkerResult>> HandleAsync(ResumeRecoveryOperationsCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WorkerInstanceId))
        {
            return Task.FromResult(Result<RecoveryWorkerResult>.Failure(Error.Validation("SECURITY_WORKER_INSTANCE_REQUIRED", "Worker instance id is required.")));
        }

        return Task.FromResult(Result<RecoveryWorkerResult>.Success(new RecoveryWorkerResult("NoWork", 0, _clock.UtcNow)));
    }
}
