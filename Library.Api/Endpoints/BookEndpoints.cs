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

            group.MapGet("/", async (BookService service) =>
            {
                var books = await service.GetAllAsync();

                return Results.Ok(
                    books.Select(b => b.ToResponse()));
            });

            group.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender) =>
            {
                var result = await sender.Send(new GetBookByIdQuery(id));

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            });

            group.MapPost("/", async (
                CreateBookRequest request,
                ISender sender) =>
            {
                var command = new CreateBookCommand(
                    request.Title,
                    request.Author,
                    request.Isbn,
                    request.PublishedYear,
                    request.TotalCopies);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Created($"/api/books/{result.Value.Id}", result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<CreateBookRequest>>();

            return app;
        }
    }
}