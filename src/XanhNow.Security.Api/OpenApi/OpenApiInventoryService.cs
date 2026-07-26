using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace XanhNow.Security.Api.OpenApi;

public sealed record OpenApiDocument(string Openapi, string InfoTitle, string InfoVersion, IReadOnlyCollection<OpenApiRoute> Routes);
public sealed record OpenApiRoute(string Method, string Path, string Controller, string Action, bool InternalOnly);

public sealed class OpenApiInventoryService
{
    private readonly EndpointDataSource _endpoints;

    public OpenApiInventoryService(EndpointDataSource endpoints)
    {
        _endpoints = endpoints;
    }

    public OpenApiDocument Create(string documentName)
    {
        var includeInternal = string.Equals(documentName, "internal-v1", StringComparison.OrdinalIgnoreCase);
        var routes = _endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => new
            {
                Endpoint = endpoint,
                HttpMethods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? Array.Empty<string>(),
                Action = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            })
            .Where(x => x.Action is not null)
            .SelectMany(x => x.HttpMethods.Select(method => new OpenApiRoute(
                method,
                NormalizePath(x.Endpoint.RoutePattern.RawText),
                x.Action!.ControllerName,
                x.Action.ActionName,
                x.Endpoint.Metadata.GetMetadata<AuthorizeAttribute>()?.Policy == "security.internal")))
            .Where(x => includeInternal || !x.InternalOnly)
            .OrderBy(x => x.Path)
            .ThenBy(x => x.Method)
            .ToArray();

        return new OpenApiDocument("3.1.0", "XanhNow.Security", documentName, routes);
    }

    private static string NormalizePath(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        return rawText.StartsWith("/", StringComparison.Ordinal) ? rawText : "/" + rawText;
    }
}

