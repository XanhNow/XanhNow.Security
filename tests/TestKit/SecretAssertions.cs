namespace XanhNow.Security.Tests.TestKit;

public static class SecretAssertions
{
    private static readonly string[] ForbiddenFragments =
    [
        "Password=",
        "PRIVATE KEY",
        "BEGIN RSA",
        "BEGIN EC",
        "totp_secret",
        "secret_id=",
        "secret-id="
    ];

    public static void DoesNotContainSecretMaterial(string value)
    {
        foreach (var fragment in ForbiddenFragments)
        {
            Assert.DoesNotContain(fragment, value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
