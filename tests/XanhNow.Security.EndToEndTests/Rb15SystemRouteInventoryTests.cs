using System.Reflection;
using XanhNow.Security.Contracts;
using XanhNow.Security.Tests.TestKit;

namespace XanhNow.Security.EndToEndTests;

public sealed class Rb15SystemRouteInventoryTests
{
    [Fact]
    [Trait(TestTraits.Category, TestCategories.EndToEnd)]
    public void Rb15_public_routes_are_unique_and_cover_all_security_surfaces()
    {
        var routes = AllRoutes();

        Assert.True(routes.Length >= 40);
        Assert.Equal(routes.Length, routes.Distinct(StringComparer.Ordinal).Count());

        Assert.All(routes, route =>
        {
            Assert.StartsWith("/", route, StringComparison.Ordinal);
            Assert.DoesNotContain("//", route, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.EndToEnd)]
    public void Rb15_system_routes_cover_required_user_security_flows()
    {
        var routes = AllRoutes();
        var requiredRoutes = new[]
        {
            ApiRoutes.Auth.Register,
            ApiRoutes.Auth.PasswordLogin,
            ApiRoutes.Auth.CompleteMfaLogin,
            ApiRoutes.Auth.PasskeyLoginBegin,
            ApiRoutes.Auth.PasskeyLoginFinish,
            ApiRoutes.Sessions.Refresh,
            ApiRoutes.Sessions.Logout,
            ApiRoutes.Sessions.LogoutAll,
            ApiRoutes.Passkeys.RegistrationBegin,
            ApiRoutes.Passkeys.RegistrationFinish,
            ApiRoutes.Passkeys.Revoke,
            ApiRoutes.SmartOtp.EnrollBegin,
            ApiRoutes.SmartOtp.EnrollConfirm,
            ApiRoutes.SmartOtp.StepUpStart,
            ApiRoutes.SmartOtp.StepUpVerify,
            ApiRoutes.Password.Change,
            ApiRoutes.Password.ResetStart,
            ApiRoutes.Password.ResetComplete,
            ApiRoutes.Password.ForceChange,
            ApiRoutes.Phone.ChangeStart,
            ApiRoutes.Phone.ChangeConfirm,
            ApiRoutes.Phone.ChangeCancel,
            ApiRoutes.Account.SecurityProfile,
            ApiRoutes.Account.Lock,
            ApiRoutes.Account.Unlock,
            ApiRoutes.Account.Disable,
            ApiRoutes.Recovery.Cases,
            ApiRoutes.Recovery.LostDevice,
            ApiRoutes.Policy.Evaluate,
            ApiRoutes.Policy.Decisions
        };

        foreach (var requiredRoute in requiredRoutes)
        {
            Assert.Contains(requiredRoute, routes);
        }
    }

    [Fact]
    [Trait(TestTraits.Category, TestCategories.EndToEnd)]
    public void Rb15_public_routes_do_not_expose_child_app_or_secret_material()
    {
        foreach (var route in AllRoutes())
        {
            Assert.DoesNotContain("auth-login", route, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("jwt-refresh-token", route, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("passkey-provider", route, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("smartotp-app", route, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", route, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string[] AllRoutes()
    {
        return typeof(ApiRoutes)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
