using Grpc.Core;
using Grpc.Net.Client;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Infrastructure.Integration.Common;
using XanhNow.Security.Infrastructure.Integration.Options;
using PasskeyGrpc = XanhNow.PasskeyProvider.Grpc.Contracts;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps.Passkey;

internal sealed class PasskeyGrpcFacadeClient : IPasskeyClient, IDisposable
{
    private readonly ChildAppClientOptions _options;
    private readonly GrpcChannel _channel;
    private readonly PasskeyGrpc.PasskeyProvider.PasskeyProviderClient _client;

    public PasskeyGrpcFacadeClient(SecurityIntegrationOptions options)
    {
        _options = options.Passkey;
        _channel = GrpcChannel.ForAddress(_options.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                EnableMultipleHttp2Connections = true
            }
        });
        _client = new PasskeyGrpc.PasskeyProvider.PasskeyProviderClient(_channel);
    }

    public async ValueTask<ChildCallResult<PasskeyBeginResult>> BeginAsync(PasskeyBeginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.Equals(request.Purpose, "login", StringComparison.OrdinalIgnoreCase))
            {
                var login = await _client.BeginPasskeyLoginAsync(new PasskeyGrpc.BeginPasskeyLoginRequest
                {
                    UsernameHint = request.LoginIdentifier ?? string.Empty,
                    RequestId = Guid.NewGuid().ToString("N")
                }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

                return ChildCallResult<PasskeyBeginResult>.Success(new PasskeyBeginResult(login.CeremonyId, login.PublicKeyCredentialRequestOptionsJson));
            }

            var registration = await _client.BeginRegisterPasskeyAsync(new PasskeyGrpc.BeginRegisterPasskeyRequest
            {
                ExternalUserId = request.UserId.ToString("D"),
                Username = request.UserId.ToString("D"),
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.UserId.ToString("D") : request.DisplayName,
                Device = ToDevice(request.Device),
                RequestId = Guid.NewGuid().ToString("N")
            }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<PasskeyBeginResult>.Success(new PasskeyBeginResult(registration.CeremonyId, registration.PublicKeyCredentialCreationOptionsJson));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<PasskeyBeginResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<PasskeyBeginResult>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public async ValueTask<ChildCallResult<PasskeyFinishResult>> FinishAsync(PasskeyFinishRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == Guid.Empty)
            {
                var login = await _client.FinishPasskeyLoginAsync(new PasskeyGrpc.FinishPasskeyLoginRequest
                {
                    CeremonyId = request.CeremonyId,
                    AssertionResponseJson = request.ClientResponseJson,
                    Device = ToDevice(request.Device),
                    RequestId = Guid.NewGuid().ToString("N")
                }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

                return ChildCallResult<PasskeyFinishResult>.Success(new PasskeyFinishResult(ParseGuidOrEmpty(login.ExternalUserId), login.CredentialId, login.Amr));
            }

            var registration = await _client.FinishRegisterPasskeyAsync(new PasskeyGrpc.FinishRegisterPasskeyRequest
            {
                CeremonyId = request.CeremonyId,
                ExternalUserId = request.UserId.ToString("D"),
                AttestationResponseJson = request.ClientResponseJson,
                Device = ToDevice(request.Device),
                RequestId = Guid.NewGuid().ToString("N")
            }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<PasskeyFinishResult>.Success(new PasskeyFinishResult(request.UserId, registration.PasskeyId, registration.Status));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<PasskeyFinishResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<PasskeyFinishResult>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public async ValueTask<ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.ListUserPasskeysAsync(new PasskeyGrpc.ListUserPasskeysRequest
            {
                ExternalUserId = userId.ToString("D")
            }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>.Success(
                response.Items.Select(x => new PasskeyDescriptor(x.PasskeyId, string.IsNullOrWhiteSpace(x.DeviceName) ? x.PasskeyId : x.DeviceName, !string.Equals(x.Status, "active", StringComparison.OrdinalIgnoreCase))).ToArray());
        }
        catch (RpcException ex)
        {
            return ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<IReadOnlyCollection<PasskeyDescriptor>>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public async ValueTask<ChildCallResult<bool>> RevokeAsync(Guid userId, string credentialId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.RevokePasskeyAsync(new PasskeyGrpc.RevokePasskeyRequest
            {
                PasskeyId = credentialId,
                ExternalUserId = userId.ToString("D"),
                Reason = "passkey_revoked",
                RequestedBy = userId.ToString("D"),
                RequestId = Guid.NewGuid().ToString("N")
            }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<bool>.Success(response.Success);
        }
        catch (RpcException ex)
        {
            return ChildCallResult<bool>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<bool>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public async ValueTask<ChildCallResult<PasskeyRevokeAllResult>> RevokeAllAsync(PasskeyRevokeAllRequest request, CancellationToken cancellationToken)
    {
        var listed = await ListAsync(request.UserId, cancellationToken);
        if (listed.IsFailure || listed.Value is null)
        {
            return ChildCallResult<PasskeyRevokeAllResult>.Failure(listed.Error ?? new ChildCallError("passkey.list_failed", "Passkey list failed before revoke-all.", true));
        }

        var revokedCount = 0;
        foreach (var credential in listed.Value.Where(x => !x.Revoked))
        {
            var revoked = await RevokeAsync(request.UserId, credential.CredentialId, cancellationToken);
            if (revoked.IsFailure || !revoked.Value)
            {
                return ChildCallResult<PasskeyRevokeAllResult>.Failure(revoked.Error ?? new ChildCallError("passkey.revoke_failed", "Passkey revoke failed during revoke-all.", true));
            }

            revokedCount++;
        }

        return ChildCallResult<PasskeyRevokeAllResult>.Success(new PasskeyRevokeAllResult(revokedCount, DateTimeOffset.UtcNow));
    }

    public ValueTask<ChildCallResult<bool>> RenameAsync(PasskeyRenameRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(ChildCallResult<bool>.Failure(new ChildCallError("passkey.rename_unsupported", "Passkey provider contract does not support rename.", false)));

    public ValueTask<ChildCallResult<bool>> SetEnabledAsync(PasskeyStateChangeRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(ChildCallResult<bool>.Failure(new ChildCallError("passkey.state_unsupported", "Passkey provider contract does not support enable or disable.", false)));

    public void Dispose() => _channel.Dispose();

    private static PasskeyGrpc.DeviceInfo ToDevice(PasskeyDeviceContext? device)
        => new()
        {
            DeviceId = device?.DeviceId ?? string.Empty,
            DeviceName = device?.DeviceName ?? string.Empty,
            Platform = device?.Platform ?? string.Empty,
            IpAddress = device?.IpAddress ?? string.Empty,
            UserAgent = device?.UserAgent ?? string.Empty
        };

    private static Guid ParseGuidOrEmpty(string value)
        => Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;

    private ChildCallError ToError(RpcException ex)
        => new("downstream.grpc_error", $"{_options.Name} gRPC {ex.StatusCode}: {ex.Status.Detail}", ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.ResourceExhausted);
}
