using Library.Api.Common.Filters;
using Library.Api.Common.Http;
using Library.Application.Borrowings.Commands.BorrowBook;
using Library.Application.Borrowings.Commands.ReturnBook;
using Library.Application.Borrowings.Queries.GetAllBorrowings;
using Library.Application.Borrowings.Queries.GetBorrowingById;
using Library.Application.Contracts.Borrowings;
using MediatR;

namespace Library.Api.Endpoints
{
    public static class BorrowingEndpoints
    {
        public static IEndpointRouteBuilder MapBorrowingEndpoints(
            this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/borrowings")
                .WithTags("Borrowings");

            group.MapGet("/", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var borrowings = await sender.Send(new GetAllBorrowingsQuery(), cancellationToken);

                return Results.Ok(borrowings);
            })
            .RequireAuthorization("AdminOnly");

            group.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetBorrowingByIdQuery(id), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            })
            .RequireAuthorization("OwnBorrowing");

            group.MapPost("/", async (
                CreateBorrowingRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new BorrowBookCommand(
                    request.MemberId,
                    request.BookId);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Created($"/api/borrowings/{result.Value.Id}", result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<CreateBorrowingRequest>>()
            .RequireAuthorization();

            group.MapPost("/{id:guid}/return", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ReturnBookCommand(id), cancellationToken);

                return result.IsSuccess
                    ? Results.NoContent()
                    : result.ToProblemDetails();
            })
            .RequireAuthorization("OwnBorrowing");

            return app;
        }
    }
}