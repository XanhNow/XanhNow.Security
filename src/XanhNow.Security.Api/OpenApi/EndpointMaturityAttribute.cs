using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace XanhNow.Security.Api.OpenApi;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class EndpointMaturityAttribute : Attribute
{
    public EndpointMaturityAttribute(string maturity, string contractId)
    {
        Maturity = maturity;
        ContractId = contractId;
    }

    public string Maturity { get; }
    public string ContractId { get; }
}

public sealed class EndpointMaturityGuardConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var plannedActions = application.Controllers
            .SelectMany(c => c.Actions)
            .Where(a => a.Attributes.OfType<EndpointMaturityAttribute>().Any(x => string.Equals(x.Maturity, "Planned", StringComparison.OrdinalIgnoreCase)))
            .Select(a => $"{a.Controller.ControllerName}.{a.ActionName}")
            .ToArray();

        if (plannedActions.Length > 0)
        {
            throw new InvalidOperationException($"PLANNED endpoints must not be published: {string.Join(", ", plannedActions)}");
        }
    }
}
