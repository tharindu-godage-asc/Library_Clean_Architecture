using MediatR;

namespace Library.Application.Abstractions.Messaging
{
    public interface IQuery<TResponse> : IRequest<TResponse>
    {
    }
}
