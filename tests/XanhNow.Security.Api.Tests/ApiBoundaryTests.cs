using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using XanhNow.Security.Api.Controllers;
using XanhNow.Security.Application.Abstractions.ChildApps.AuthLogin;
using XanhNow.Security.Application.Abstractions.ChildApps.Jwt;
using XanhNow.Security.Application.Abstractions.ChildApps.Passkey;
using XanhNow.Security.Application.Abstractions.ChildApps.SmartOtp;
using XanhNow.Security.Infrastructure.Persistence;

namespace XanhNow.Security.Api.Tests;

public sealed class ApiBoundaryTests
{
    [Fact]
    public void Rb13_publishes_core_and_account_security_vertical_slice_actions()
    {
        var shellTypes = typeof(Program).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(ApiControllerBase)))
            .Where(type => type.Name is not nameof(HealthController))
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.Equal(new[]
        {
            "AccountController",
            "AuthController",
            "OperationsController",
            "PasskeysController",
            "PasswordController",
            "PhoneController",
            "PolicyController",
            "RecoveryController",
            "SessionsController",
            "SmartOtpController"
        }, shellTypes.Select(type => type.Name).ToArray());

        var expectedActionCounts = new Dictionary<string, int>
        {
            ["AccountController"] = 4,
            ["AuthController"] = 4,
            ["PasskeysController"] = 7,
            ["PasswordController"] = 4,
            ["PhoneController"] = 3,
            ["SessionsController"] = 4,
            ["SmartOtpController"] = 4
        };

        foreach (var shell in shellTypes)
        {
            var actions = shell.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
                .ToArray();
            Assert.Equal(expectedActionCounts.GetValueOrDefault(shell.Name), actions.Length);
        }
    }

    [Fact]
    public void Controllers_do_not_inject_persistence_or_child_app_clients()
    {
        var forbidden = new[]
        {
            typeof(SecurityDbContext),
            typeof(IAuthLoginClient),
            typeof(IJwtTokenClient),
            typeof(IPasskeyClient),
            typeof(ISmartOtpClient)
        };

        var controllerTypes = typeof(Program).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(ControllerBase)));

        foreach (var controller in controllerTypes)
        {
            var constructorParameters = controller.GetConstructors().SelectMany(ctor => ctor.GetParameters()).Select(parameter => parameter.ParameterType);
            Assert.DoesNotContain(constructorParameters, parameterType => forbidden.Contains(parameterType));
        }
    }
}
