using Library.Api.Common.Filters;
using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Services;
using Library.Domain.Entities;

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
                BookService service) =>
            {
                var book = await service.GetByIdAsync(id);

                return Results.Ok(book.ToResponse());
            });

            group.MapPost("/", async (
                CreateBookRequest request,
                BookService service) =>
            {
                var book = new Book(
                    request.Title,
                    request.Author,
                    request.Isbn,
                    request.PublishedYear,
                    request.TotalCopies);

                await service.CreateAsync(book);

                return Results.Created(
                    $"/api/books/{book.Id}",
                    book.ToResponse());
            })
            .AddEndpointFilter<ValidationFilter<CreateBookRequest>>();

            return app;
        }
    }
}