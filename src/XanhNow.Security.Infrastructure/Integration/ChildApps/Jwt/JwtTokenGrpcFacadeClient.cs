using Grpc.Core;
using Grpc.Net.Client;
using GrpcTokenProvider.Grpc;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Infrastructure.Integration.Options;
using XanhNow.Security.Infrastructure.Integration.Common;

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

    public async ValueTask<ChildCallResult<bool>> RevokeSessionAsync(JwtRevokeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.RevokeTokenAsync(
                new RevokeTokenRequest { RefreshToken = request.SessionId },
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

    public ValueTask<ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>> ListSessionsAsync(Guid userId, CancellationToken cancellationToken)
        => ValueTask.FromResult(ChildCallResult<IReadOnlyCollection<JwtSessionDescriptor>>.Success(Array.Empty<JwtSessionDescriptor>()));

    public ValueTask<ChildCallResult<JwtRevokeAllResult>> RevokeAllSessionsAsync(JwtRevokeAllRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(ChildCallResult<JwtRevokeAllResult>.Success(new JwtRevokeAllResult(0, DateTimeOffset.UtcNow)));

    public void Dispose() => _channel.Dispose();

    private static JwtIssueResult ToIssueResult(TokenPairResponse response)
        => new(
            response.Jwt,
            response.RefreshToken,
            response.JwtExpiry.ToDateTimeOffset());

    private ChildCallError ToError(RpcException ex)
        => new(
            "downstream.grpc_error",
            $"{_options.Name} gRPC {ex.StatusCode}: {ex.Status.Detail}",
            ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.ResourceExhausted);
}
