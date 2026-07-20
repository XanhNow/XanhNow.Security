namespace XanhNow.Security.Contracts;

public static class ApiRoutes
{
    public static class Auth
    {
        public const string Register = "/api/v1/auth/register";
        public const string PasswordLogin = "/api/v1/auth/login/password";
        public const string CompleteMfaLogin = "/api/v1/auth/login/mfa/complete";
        public const string PasskeyLoginBegin = "/api/v1/auth/login/passkey/begin";
        public const string PasskeyLoginFinish = "/api/v1/auth/login/passkey/finish";
    }

    public static class Account
    {
        public const string SecurityProfile = "/api/v1/accounts/me/security-profile";
        public const string Lock = "/api/v1/accounts/{userId}/lock";
        public const string Unlock = "/api/v1/accounts/{userId}/unlock";
        public const string Disable = "/api/v1/accounts/{userId}/disable";
    }

    public static class Password
    {
        public const string Change = "/api/v1/password/change";
        public const string ResetStart = "/api/v1/password/reset/start";
        public const string ResetComplete = "/api/v1/password/reset/complete";
        public const string ForceChange = "/api/v1/password/force-change";
    }

    public static class Phone
    {
        public const string ChangeStart = "/api/v1/phone/change/start";
        public const string ChangeConfirm = "/api/v1/phone/change/confirm";
        public const string ChangeCancel = "/api/v1/phone/change/cancel";
        public const string ChangeHistory = "/api/v1/phone/change/history";
    }

    public static class Recovery
    {
        public const string Cases = "/api/v1/recovery/cases";
        public const string LostDevice = "/api/v1/recovery/lost-device";
        public const string CaseStatus = "/api/v1/recovery/cases/{recoveryCaseId}";
    }

    public static class Sessions
    {
        public const string Refresh = "/api/v1/sessions/refresh";
        public const string List = "/api/v1/sessions";
        public const string Logout = "/api/v1/sessions/{sessionId}/logout";
        public const string LogoutAll = "/api/v1/sessions/logout-all";
    }

    public static class Passkeys
    {
        public const string RegistrationBegin = "/api/v1/passkeys/registration/begin";
        public const string RegistrationFinish = "/api/v1/passkeys/registration/finish";
        public const string List = "/api/v1/passkeys";
        public const string Rename = "/api/v1/passkeys/{passkeyId}/rename";
        public const string Disable = "/api/v1/passkeys/{passkeyId}/disable";
        public const string Enable = "/api/v1/passkeys/{passkeyId}/enable";
        public const string Revoke = "/api/v1/passkeys/{passkeyId}/revoke";
    }

    public static class SmartOtp
    {
        public const string Devices = "/api/v1/smart-otp/devices";
        public const string EnrollBegin = "/api/v1/smart-otp/devices/enroll/begin";
        public const string EnrollConfirm = "/api/v1/smart-otp/devices/enroll/confirm";
        public const string RevokeDevice = "/api/v1/smart-otp/devices/{deviceId}/revoke";
        public const string StepUpStart = "/api/v1/smart-otp/step-up/start";
        public const string StepUpVerify = "/api/v1/smart-otp/step-up/verify";
    }

    public static class Policy
    {
        public const string Evaluate = "/api/v1/policies/evaluate";
        public const string Decisions = "/api/v1/policies/decisions";
    }

    public static class Health
    {
        public const string Live = "/health/live";
        public const string Ready = "/health/ready";
        public const string Dependencies = "/health/dependencies";
    }
}
