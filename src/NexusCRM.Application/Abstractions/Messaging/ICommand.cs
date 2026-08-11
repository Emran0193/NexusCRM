using MediatR;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
