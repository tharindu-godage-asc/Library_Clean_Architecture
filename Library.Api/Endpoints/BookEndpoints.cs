using Library.Api.Common.Filters;
using Library.Api.Common.Http;
using Library.Application.Books.Commands.CreateBook;
using Library.Application.Books.Commands.DeleteBook;
using Library.Application.Books.Commands.UpdateBook;
using Library.Application.Books.Queries.GetAllBooks;
using Library.Application.Books.Queries.GetBookById;
using Library.Application.Contracts.Books;
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
                string? title,
                string? author,
                int? publishedYear,
                string? sortBy,
                bool? sortDescending,
                int? pageNumber,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAllBooksQuery(
                    title,
                    author,
                    publishedYear,
                    sortBy,
                    sortDescending ?? false,
                    pageNumber ?? 1,
                    pageSize ?? 20);

                var result = await sender.Send(query, cancellationToken);

                return Results.Ok(result);
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

            group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateBookRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateBookCommand(
                    id,
                    request.Title,
                    request.Author,
                    request.Isbn,
                    request.PublishedYear,
                    request.TotalCopies);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<UpdateBookRequest>>();

            group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteBookCommand(id), cancellationToken);

                return result.IsSuccess
                    ? Results.NoContent()
                    : result.ToProblemDetails();
            });

            return app;
        }
    }
}