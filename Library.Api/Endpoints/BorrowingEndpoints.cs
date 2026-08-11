using Library.Api.Common.Filters;
using Library.Application.Contracts.Borrowings;
using Library.Application.Contracts.Mappings;
using Library.Application.Services;

namespace Library.Api.Endpoints
{
    public static class BorrowingEndpoints
    {
        public static IEndpointRouteBuilder MapBorrowingEndpoints(
            this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/borrowings")
                .WithTags("Borrowings");

            group.MapGet("/", async (BorrowingService service) =>
            {
                var borrowings = await service.GetAllAsync();

                return Results.Ok(
                    borrowings.Select(b => b.ToResponse()));
            });

            group.MapGet("/{id:int}", async (
                int id,
                BorrowingService service) =>
            {
                var borrowing = await service.GetByIdAsync(id);

                return Results.Ok(borrowing.ToResponse());
            });

            group.MapPost("/", async (
                CreateBorrowingRequest request,
                BorrowingService service) =>
            {
                var borrowing =
                    await service.BorrowBookAsync(
                        request.MemberId,
                        request.BookId);

                return Results.Created(
                    $"/api/borrowings/{borrowing.Id}",
                    borrowing.ToResponse());
            })
            .AddEndpointFilter<ValidationFilter<CreateBorrowingRequest>>();

            group.MapPost("/{id:int}/return", async (
                int id,
                BorrowingService service) =>
            {
                await service.ReturnBookAsync(id);

                return Results.NoContent();
            });

            return app;
        }
    }
}