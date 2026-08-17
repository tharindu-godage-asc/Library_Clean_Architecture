using System;
using System.Collections.Generic;
using System.Text;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Abstractions.Messaging
{
    public interface ICommand: IRequest<Result>
    {
    }

    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {

    }
}
