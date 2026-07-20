namespace XanhNow.Security.Application.Common.Requests;

public interface IApplicationRequest<TResponse>;

public interface ICommand<TResponse> : IApplicationRequest<TResponse>;

public interface IQuery<TResponse> : IApplicationRequest<TResponse>;
