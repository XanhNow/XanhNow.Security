using XanhNow.Security.Contracts.Common.Responses;

namespace XanhNow.Security.Contracts.Common.Errors;

public sealed record ApiErrorResponse(string Code, string Message, IReadOnlyCollection<ApiErrorDetail> Details, ApiResponseMetadata Metadata);
