using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Tests.Common;

public sealed class ApplicationExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_RunsBehaviorsInConfiguredOrderThenHandler()
    {
        var calls = new List<string>();
        var executor = new ApplicationExecutor<TestRequest, string>(
            new TestHandler(calls),
            [new OrderedBehavior<TestRequest, string>("one", calls), new OrderedBehavior<TestRequest, string>("two", calls)]);

        var result = await executor.ExecuteAsync(new TestRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["one-before", "two-before", "handler", "two-after", "one-after"], calls);
    }

    private sealed record TestRequest : ICommand<string>;

    private sealed class TestHandler : IRequestHandler<TestRequest, string>
    {
        private readonly List<string> _calls;
        public TestHandler(List<string> calls) => _calls = calls;
        public Task<Result<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            _calls.Add("handler");
            return Task.FromResult(Result<string>.Success("ok"));
        }
    }

    private sealed class OrderedBehavior<TRequest, TResponse> : IApplicationBehavior<TRequest, TResponse>
        where TRequest : IApplicationRequest<TResponse>
    {
        private readonly string _name;
        private readonly List<string> _calls;
        public OrderedBehavior(string name, List<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public async Task<Result<TResponse>> HandleAsync(TRequest request, ApplicationHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _calls.Add($"{_name}-before");
            var result = await next(cancellationToken);
            _calls.Add($"{_name}-after");
            return result;
        }
    }
}
