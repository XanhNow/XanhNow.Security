using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XanhNow.Security.Api.OpenApi;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Core;
using XanhNow.Security.Contracts.Common.Responses;
using XanhNow.Security.Contracts.V1.Passkey;

namespace XanhNow.Security.Api.Controllers;

[Authorize]
[Route("api/v1/passkeys")]
public sealed class PasskeysController : ApiControllerBase
{
    private readonly ApplicationExecutor<BeginPasskeyRegistrationCommand, BeginPasskeyRegistrationResult> _beginRegistration;
    private readonly ApplicationExecutor<FinishPasskeyRegistrationCommand, PasskeyStateResult> _finishRegistration;
    private readonly ApplicationExecutor<ListPasskeysQuery, IReadOnlyCollection<PasskeySummaryResult>> _list;
    private readonly ApplicationExecutor<RevokePasskeyCommand, PasskeyStateResult> _revoke;
    private readonly ApplicationExecutor<RenamePasskeyCommand, PasskeyStateResult> _rename;
    private readonly ApplicationExecutor<SetPasskeyEnabledCommand, PasskeyStateResult> _setEnabled;

    public PasskeysController(
        ApplicationExecutor<BeginPasskeyRegistrationCommand, BeginPasskeyRegistrationResult> beginRegistration,
        ApplicationExecutor<FinishPasskeyRegistrationCommand, PasskeyStateResult> finishRegistration,
        ApplicationExecutor<ListPasskeysQuery, IReadOnlyCollection<PasskeySummaryResult>> list,
        ApplicationExecutor<RevokePasskeyCommand, PasskeyStateResult> revoke,
        ApplicationExecutor<RenamePasskeyCommand, PasskeyStateResult> rename,
        ApplicationExecutor<SetPasskeyEnabledCommand, PasskeyStateResult> setEnabled)
    {
        _beginRegistration = beginRegistration;
        _finishRegistration = finishRegistration;
        _list = list;
        _revoke = revoke;
        _rename = rename;
        _setEnabled = setEnabled;
    }

    [HttpPost("registration/begin")]
    [EndpointMaturity("Current", "passkeys.registration.begin")]
    public async Task<ActionResult<ApiResponse<BeginPasskeyRegistrationResponse>>> BeginRegistrationAsync(BeginPasskeyRegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await _beginRegistration.ExecuteAsync(new BeginPasskeyRegistrationCommand(CurrentUserIdOrEmpty(), request.DisplayName), cancellationToken);
        return FromApplicationResult(result, x => new BeginPasskeyRegistrationResponse(x.CeremonyId, x.PublicKeyOptions, x.ExpiresAtUtc));
    }

    [HttpPost("registration/finish")]
    [EndpointMaturity("Current", "passkeys.registration.finish")]
    public async Task<ActionResult<ApiResponse<PasskeyStateResponse>>> FinishRegistrationAsync(FinishPasskeyRegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await _finishRegistration.ExecuteAsync(new FinishPasskeyRegistrationCommand(CurrentUserIdOrEmpty(), request.CeremonyId, request.Credential, request.DeviceName), cancellationToken);
        return FromApplicationResult(result, MapState);
    }

    [HttpGet]
    [EndpointMaturity("Current", "passkeys.list")]
    public async Task<ActionResult<ApiResponse<PasskeySummaryResponse[]>>> ListAsync(CancellationToken cancellationToken)
    {
        var result = await _list.ExecuteAsync(new ListPasskeysQuery(CurrentUserIdOrEmpty()), cancellationToken);
        return FromApplicationResult(result, x => x.Select(p => new PasskeySummaryResponse(p.PasskeyId, p.DisplayName, p.DeviceName, p.IsEnabled, p.CreatedAtUtc, p.LastUsedAtUtc)).ToArray());
    }

    [HttpPost("{passkeyId}/revoke")]
    [EndpointMaturity("Current", "passkeys.revoke")]
    public async Task<ActionResult<ApiResponse<PasskeyStateResponse>>> RevokeAsync(string passkeyId, RevokePasskeyRequest request, CancellationToken cancellationToken)
    {
        var result = await _revoke.ExecuteAsync(new RevokePasskeyCommand(CurrentUserIdOrEmpty(), passkeyId, request.ReasonCode), cancellationToken);
        return FromApplicationResult(result, MapState);
    }

    [HttpPost("{passkeyId}/rename")]
    [EndpointMaturity("Current", "passkeys.rename")]
    public async Task<ActionResult<ApiResponse<PasskeyStateResponse>>> RenameAsync(string passkeyId, RenamePasskeyRequest request, CancellationToken cancellationToken)
    {
        var result = await _rename.ExecuteAsync(new RenamePasskeyCommand(CurrentUserIdOrEmpty(), passkeyId, request.DisplayName), cancellationToken);
        return FromApplicationResult(result, MapState);
    }

    [HttpPost("{passkeyId}/disable")]
    [EndpointMaturity("Current", "passkeys.disable")]
    public async Task<ActionResult<ApiResponse<PasskeyStateResponse>>> DisableAsync(string passkeyId, CancellationToken cancellationToken)
    {
        var result = await _setEnabled.ExecuteAsync(new SetPasskeyEnabledCommand(CurrentUserIdOrEmpty(), passkeyId, false, "passkey_disabled"), cancellationToken);
        return FromApplicationResult(result, MapState);
    }

    [HttpPost("{passkeyId}/enable")]
    [EndpointMaturity("Current", "passkeys.enable")]
    public async Task<ActionResult<ApiResponse<PasskeyStateResponse>>> EnableAsync(string passkeyId, CancellationToken cancellationToken)
    {
        var result = await _setEnabled.ExecuteAsync(new SetPasskeyEnabledCommand(CurrentUserIdOrEmpty(), passkeyId, true, "passkey_enabled"), cancellationToken);
        return FromApplicationResult(result, MapState);
    }

    private static PasskeyStateResponse MapState(PasskeyStateResult state)
        => new(state.PasskeyId, state.IsEnabled, state.UpdatedAtUtc);
}
