namespace XanhNow.Security.Tests.TestKit.ChildApps;

public sealed record ChildAppContractTarget(string AppName, string Protocol, string RuntimePort, string ContractOwner);

public static class ChildAppContractCatalog
{
    public static IReadOnlyList<ChildAppContractTarget> All { get; } =
    [
        new("Auth_Login_App", "REST", "8080", "auth-login"),
        new("JWT_Refresh_Token_App", "gRPC facade", "5102", "jwt-refresh-token"),
        new("Passkey_Provider_App", "gRPC + mTLS facade", "5101", "passkey-provider"),
        new("SmartOtp_App", "gRPC + mTLS facade", "5104", "smart-otp")
    ];
}
