using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Infrastructure.Integration.Common;
using XanhNow.Security.Infrastructure.Integration.Options;
using SmartOtpGrpc = XanhNow.Auth.SmartOtp.Grpc.Contracts;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps.SmartOtp;

internal sealed class SmartOtpGrpcMtlsClient : ISmartOtpClient, IDisposable
{
    private readonly ChildAppClientOptions options;
    private readonly GrpcChannel channel;
    private readonly SmartOtpGrpc.SmartOtpProvider.SmartOtpProviderClient client;

    public SmartOtpGrpcMtlsClient(SecurityIntegrationOptions integrationOptions)
    {
        options = integrationOptions.SmartOtp;
        channel = GrpcChannel.ForAddress(options.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = CreateHandler(options)
        });
        client = new SmartOtpGrpc.SmartOtpProvider.SmartOtpProviderClient(channel);
    }

    public async ValueTask<ChildCallResult<SmartOtpBindBeginResult>> BeginBindAsync(SmartOtpBindBeginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.BeginBindOtpDeviceAsync(new SmartOtpGrpc.BeginBindOtpDeviceRequest
            {
                ExternalUserId = ToExternalUserId(request.UserId),
                DeviceName = request.DeviceName,
                Platform = request.Platform,
                AppInstanceIdHash = ByteString.CopyFrom(FromBase64(request.AppInstanceIdHashBase64)),
                KeyAlgorithm = request.KeyAlgorithm,
                CandidatePublicKeySpki = ByteString.CopyFrom(FromBase64(request.CandidatePublicKeySpkiBase64)),
                CandidatePublicKeyThumbprint = ByteString.CopyFrom(FromBase64(request.CandidatePublicKeyThumbprintBase64)),
                Metadata = NewMetadata("smart-otp-bind-begin")
            }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<SmartOtpBindBeginResult>.Success(new SmartOtpBindBeginResult(
                response.BindingId,
                Convert.ToBase64String(response.ServerChallenge.ToByteArray()),
                response.ChallengeFormatVersion,
                DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresAtUnixMs),
                response.Status));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<SmartOtpBindBeginResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<SmartOtpBindBeginResult>.Failure(DownstreamErrorMapper.FromException(ex, options.Name));
        }
    }

    public async ValueTask<ChildCallResult<SmartOtpBindFinishResult>> FinishBindAsync(SmartOtpBindFinishRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.CompleteBindOtpDeviceAsync(new SmartOtpGrpc.CompleteBindOtpDeviceRequest
            {
                BindingId = request.BindingId,
                ExternalUserId = ToExternalUserId(request.UserId),
                ClientNonce = ByteString.CopyFrom(FromBase64(request.ClientNonceBase64)),
                DeviceSignature = ByteString.CopyFrom(FromBase64(request.DeviceSignatureBase64)),
                Metadata = NewMetadata("smart-otp-bind-finish")
            }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<SmartOtpBindFinishResult>.Success(new SmartOtpBindFinishResult(
                response.DeviceId,
                response.DeviceKeyId,
                response.Status,
                DateTimeOffset.FromUnixTimeMilliseconds(response.BoundAtUnixMs)));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<SmartOtpBindFinishResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<SmartOtpBindFinishResult>.Failure(DownstreamErrorMapper.FromException(ex, options.Name));
        }
    }

    public async ValueTask<ChildCallResult<SmartOtpChallengeResult>> CreateChallengeAsync(SmartOtpChallengeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.CreateOtpChallengeAsync(new SmartOtpGrpc.CreateOtpChallengeRequest
            {
                ExternalUserId = ToExternalUserId(request.UserId),
                Purpose = request.Purpose,
                PolicyCode = request.Purpose,
                TransactionDigest = ByteString.CopyFrom(Encoding.UTF8.GetBytes(request.TransactionSummary ?? string.Empty)),
                Metadata = NewMetadata("smart-otp-challenge-create")
            }, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);

            return ChildCallResult<SmartOtpChallengeResult>.Success(
                new SmartOtpChallengeResult(response.ChallengeId, DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiresAtUnixMs)));
        }
        catch (RpcException ex)
        {
            return ChildCallResult<SmartOtpChallengeResult>.Failure(ToError(ex));
        }
        catch (Exception ex)
        {
            return ChildCallResult<SmartOtpChallengeResult>.Failure(DownstreamErrorMapper.FromException(ex, options.Name));
        }
    }

    public ValueTask<ChildCallResult<SmartOtpVerifyResult>> VerifyAsync(SmartOtpVerifyRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(ChildCallResult<SmartOtpVerifyResult>.Failure(new ChildCallError("smart_otp.contract_incomplete", "Smart OTP verify requires device id and transaction context.", false)));

    public ValueTask<ChildCallResult<SmartOtpRevokeAllDevicesResult>> RevokeAllDevicesAsync(SmartOtpRevokeAllDevicesRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(ChildCallResult<SmartOtpRevokeAllDevicesResult>.Failure(new ChildCallError("smart_otp.revoke_all_unsupported", "Smart OTP provider contract does not support revoke-all devices without device ids.", false)));

    public void Dispose() => channel.Dispose();

    private static SocketsHttpHandler CreateHandler(ChildAppClientOptions options)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        };

        if (options.RequiresMtls)
        {
            var certificate = X509Certificate2.CreateFromPemFile(options.ClientCertificatePath!, options.ClientCertificateKeyPath!);
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                ClientCertificates = new X509CertificateCollection { certificate },
                RemoteCertificateValidationCallback = (_, certificate, _, errors) => ValidateServerCertificate(certificate, errors, options.TrustedCaPath!)
            };
        }

        return handler;
    }

    private static bool ValidateServerCertificate(X509Certificate? certificate, SslPolicyErrors errors, string caPath)
    {
        if (certificate is null || (errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
        {
            return false;
        }

        using var serverCertificate = new X509Certificate2(certificate);
        using var ca = X509CertificateLoader.LoadCertificateFromFile(caPath);
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(ca);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(serverCertificate);
    }


    private static string ToExternalUserId(Guid userId)
    {
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        Span<byte> bytes = stackalloc byte[16];
        userId.TryWriteBytes(bytes);

        Span<char> chars = stackalloc char[26];
        var value = new UInt128(
            BitConverter.ToUInt64(bytes[..8]),
            BitConverter.ToUInt64(bytes[8..]));

        for (var i = 25; i >= 0; i--)
        {
            chars[i] = alphabet[(int)(value & 31)];
            value >>= 5;
        }

        return new string(chars);
    }

    private static SmartOtpGrpc.RequestMetadata NewMetadata(string step) => new()
    {
        OriginServiceId = "xanhnow-auth-login",
        CorrelationId = $"security-{Guid.NewGuid():N}"[..32],
        RequestId = $"{step}-{Guid.NewGuid():N}",
        IdempotencyKey = $"idem-{step}-{Guid.NewGuid():N}"
    };

    private static byte[] FromBase64(string value) => Convert.FromBase64String(value);

    private ChildCallError ToError(RpcException ex)
        => new("downstream.grpc_error", $"{options.Name} gRPC {ex.StatusCode}: {ex.Status.Detail}", ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.ResourceExhausted);
}
