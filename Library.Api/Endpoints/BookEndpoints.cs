using Library.Api.Common.Filters;
using Library.Api.Common.Http;
using Library.Application.Books.Commands.CreateBook;
using Library.Application.Books.Queries.GetBookById;
using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Services;
using MediatR;

namespace Library.Api.Endpoints
{
    public static class BookEndpoints
    {
        public static IEndpointRouteBuilder MapBookEndpoints(
            this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/books")
                .WithTags("Books");

            group.MapGet("/", async (
                BookService service,
                CancellationToken cancellationToken) =>
            {
                var books = await service.GetAllAsync(cancellationToken);

                return Results.Ok(
                    books.Select(b => b.ToResponse()));
            });

            group.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetBookByIdQuery(id), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            });

            group.MapPost("/", async (
                CreateBookRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateBookCommand(
                    request.Title,
                    request.Author,
                    request.Isbn,
                    request.PublishedYear,
                    request.TotalCopies);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Created($"/api/books/{result.Value.Id}", result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<CreateBookRequest>>();

            return app;
        }
    }
}