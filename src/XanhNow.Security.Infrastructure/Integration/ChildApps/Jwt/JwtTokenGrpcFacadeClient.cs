using Grpc.Core;
using Grpc.Net.Client;
using GrpcTokenProvider.Grpc;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Infrastructure.Integration.Common;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps.Jwt;

internal sealed class JwtTokenGrpcFacadeClient : IJwtTokenClient, IDisposable
{
    private readonly ChildAppClientOptions _options;
    private readonly GrpcChannel _channel;
    private readonly TokenProvider.TokenProviderClient _client;

    public JwtTokenGrpcFacadeClient(SecurityIntegrationOptions options)
    {
        _options = options.Jwt;
        _channel = GrpcChannel.ForAddress(_options.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                EnableMultipleHttp2Connections = true
            }
        });
        _client = new TokenProvider.TokenProviderClient(_channel);
    }

    public async ValueTask<ChildCallResult<JwtIssueResult>> IssueAsync(JwtIssueRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var grpcRequest = new IssueTokenRequest
            {
                Subject = request.UserId.ToString("D")
            };
            grpcRequest.Roles.Add("user");
            grpcRequest.Scopes.AddRange(request.Scopes);

            var response = await _client.IssueTokenAsync(grpcRequest, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
            return ChildCallResult<JwtIssueResult>.Success(ToIssueResult(response));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<JwtIssueResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<JwtIssueResult>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public async ValueTask<ChildCallResult<JwtIssueResult>> RefreshAsync(JwtRefreshRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.RefreshTokenAsync(
                new RefreshTokenRequest { RefreshToken = request.RefreshTokenReference },
                cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<JwtIssueResult>.Success(ToIssueResult(response));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<JwtIssueResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<JwtIssueResult>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public async ValueTask<ChildCallResult<JwtValidateResult>> ValidateAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.ValidateTokenAsync(
                new ValidateTokenRequest { Jwt = accessToken },
                cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            if (!response.IsValid || !Guid.TryParse(response.Subject, out var userId))
            {
                return ChildCallResult<JwtValidateResult>.Success(new JwtValidateResult(false, null, [], [], null));
            }

            return ChildCallResult<JwtValidateResult>.Success(new JwtValidateResult(
                true,
                userId,
                response.Roles.ToArray(),
                response.Scopes.ToArray(),
                string.IsNullOrWhiteSpace(response.SessionId) ? null : response.SessionId));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<JwtValidateResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<JwtValidateResult>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public async ValueTask<ChildCallResult<bool>> RevokeSessionAsync(JwtRevokeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.RevokeTokenAsync(
                new RevokeTokenRequest { SessionId = request.SessionId },
                cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<bool>.Success(response.IsRevoked);
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

    public async ValueTask<ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.ListSessionsAsync(
                new ListSessionsRequest { Subject = userId.ToString("D") },
                cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            var sessions = response.Sessions
                .Where(x => Guid.TryParse(x.Subject, out _))
                .Select(x => new JwtSessionDescriptor(
                    x.SessionId,
                    Guid.Parse(x.Subject),
                    x.Status,
                    null,
                    null,
                    x.CreatedAt.ToDateTimeOffset(),
                    IsEmpty(x.LastSeenAt) ? x.CreatedAt.ToDateTimeOffset() : x.LastSeenAt.ToDateTimeOffset(),
                    x.ExpiresAt.ToDateTimeOffset()))
                .ToArray();

            return ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>.Success(sessions);
        }
        catch (RpcException ex)
        {
            return ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public async ValueTask<ChildCallResult<JwtRevokeAllResult>> RevokeAllSessionsAsync(JwtRevokeAllRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.RevokeAllSessionsAsync(
                new RevokeAllSessionsRequest
                {
                    Subject = request.UserId.ToString("D"),
                    ReasonCode = request.ReasonCode,
                    IncludeCurrentSession = request.IncludeCurrentSession,
                    CurrentSessionId = request.CurrentSessionId ?? string.Empty
                },
                cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<JwtRevokeAllResult>.Success(new JwtRevokeAllResult(
                response.RevokedCount,
                response.RevokedAt.ToDateTimeOffset()));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<JwtRevokeAllResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<JwtRevokeAllResult>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }

    public void Dispose() => _channel.Dispose();

    private static JwtIssueResult ToIssueResult(TokenPairResponse response)
        => new(
            response.Jwt,
            response.RefreshToken,
            response.JwtExpiry.ToDateTimeOffset());

    private static bool IsEmpty(Google.Protobuf.WellKnownTypes.Timestamp timestamp)
        => timestamp.Seconds == 0 && timestamp.Nanos == 0;

    private ChildCallError ToError(RpcException ex)
        => new(
            "downstream.grpc_error",
            $"{_options.Name} gRPC {ex.StatusCode}: {ex.Status.Detail}",
            ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.ResourceExhausted);
}
