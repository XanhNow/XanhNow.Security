using System.Net.Http.Json;
using XanhNow.Security.Application.Abstractions.ChildApps;
using XanhNow.Security.Infrastructure.Integration.Common;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.ChildApps;

internal abstract class ChildAppJsonClient
{
    private static readonly TimeSpan DefaultDeadline = TimeSpan.FromSeconds(8);
    private readonly HttpClient _http;
    private readonly ChildAppClientOptions _options;
    private readonly string _contractVersion;

    protected ChildAppJsonClient(HttpClient http, ChildAppClientOptions options, string contractVersion)
    {
        _http = http;
        _options = options;
        _contractVersion = contractVersion;
    }

    protected ValueTask<ChildCallResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken)
        where TResponse : notnull
        => SendAsync<TRequest, TResponse>(HttpMethod.Post, path, request, cancellationToken);

    protected ValueTask<ChildCallResult<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken)
        where TResponse : notnull
        => SendAsync<object, TResponse>(HttpMethod.Get, path, null, cancellationToken);

    private async ValueTask<ChildCallResult<TResponse>> SendAsync<TRequest, TResponse>(HttpMethod method, string path, TRequest? request, CancellationToken cancellationToken)
        where TResponse : notnull
    {
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(_options.Deadline > TimeSpan.Zero ? _options.Deadline : DefaultDeadline);
            using var message = new HttpRequestMessage(method, path);
            message.Headers.TryAddWithoutValidation("X-Contract-Version", _contractVersion);
            if (request is not null)
            {
                message.Content = JsonContent.Create(request);
            }

            using var response = await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode)
            {
                return ChildCallResult<TResponse>.Failure(DownstreamErrorMapper.FromHttpStatus(response.StatusCode, _options.Name));
            }

            var value = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: deadline.Token);
            if (value is null)
            {
                return ChildCallResult<TResponse>.Failure(new ChildCallError("downstream.empty_response", $"{_options.Name} returned empty response.", false));
            }

            return ChildCallResult<TResponse>.Success(value);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ChildCallResult<TResponse>.Failure(new ChildCallError("downstream.deadline_exceeded", $"{_options.Name} deadline exceeded.", true));
        }
        catch (HttpRequestException ex)
        {
            return ChildCallResult<TResponse>.Failure(DownstreamErrorMapper.FromException(ex, _options.Name));
        }
    }
}
