using System.Reflection;
using XanhNow.Security.Contracts;

namespace XanhNow.Security.ContractTests.Routes;

public sealed class ApiRouteTests
{
    [Fact]
    public void Route_constants_are_unique()
    {
        var routes = typeof(ApiRoutes).GetNestedTypes(BindingFlags.Public)
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        Assert.NotEmpty(routes);
        Assert.Equal(routes.Length, routes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(routes.Where(route => route.StartsWith("/api/", StringComparison.Ordinal)), route => Assert.StartsWith(ApiContractVersions.PublicApiPrefix, route));
    }
}
