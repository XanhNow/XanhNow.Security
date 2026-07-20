using XanhNow.Security.Contracts.Common.Attributes;
using XanhNow.Security.Contracts.Common.Enums;

namespace XanhNow.Security.Contracts.Common.Responses;

public sealed record ApiResponseMetadata(string ContractVersion, string CorrelationId, string RequestId, DateTimeOffset TimestampUtc);
public sealed record ApiResponse<T>(T Data, ApiResponseMetadata Metadata);
public sealed record DeviceContextRequest(string? DeviceId, string? DeviceName, string? Platform, string? IpAddress, string? UserAgent);
public sealed record TokenPairResponse([property: SensitiveData("access-token")] string AccessToken, [property: SensitiveData("refresh-token")] string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc, DateTimeOffset RefreshTokenExpiresAtUtc, string TokenType = "Bearer");
public sealed record OperationAcceptedResponse(Guid OperationId, OperationStatusContract Status, string OperationType, string CurrentStep, DateTimeOffset AcceptedAtUtc);
public sealed record OperationStatusResponse(Guid OperationId, OperationStatusContract Status, string OperationType, string CurrentStep, string? ResultCode, DateTimeOffset UpdatedAtUtc);
