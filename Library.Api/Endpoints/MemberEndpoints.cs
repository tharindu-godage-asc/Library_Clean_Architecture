using Library.Api.Common.Filters;
using Library.Api.Common.Http;
using Library.Application.Contracts.Members;
using Library.Application.Members.Commands.CreateMember;
using Library.Application.Members.Commands.DeleteMember;
using Library.Application.Members.Commands.UpdateMember;
using Library.Application.Members.Queries.GetAllMembers;
using Library.Application.Members.Queries.GetMemberById;
using Library.Application.Borrowings.Queries.GetBorrowingsByMember;
using MediatR;

namespace Library.Api.Endpoints
{
    public static class MemberEndpoints
    {
        public static IEndpointRouteBuilder MapMemberEndpoints(
            this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/members")
                .WithTags("Members");

            group.MapGet("/", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var members = await sender.Send(new GetAllMembersQuery(), cancellationToken);

                return Results.Ok(members);
            });

            group.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetMemberByIdQuery(id), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            });

            group.MapPost("/", async (
                CreateMemberRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateMemberCommand(
                    request.Name,
                    request.Email,
                    request.PhoneNumber);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Created($"/api/members/{result.Value.Id}", result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<CreateMemberRequest>>();

            group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateMemberRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateMemberCommand(
                    id,
                    request.Name,
                    request.Email,
                    request.PhoneNumber);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<UpdateMemberRequest>>();

            group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteMemberCommand(id), cancellationToken);

                return result.IsSuccess
                    ? Results.NoContent()
                    : result.ToProblemDetails();
            });

            group.MapGet("/{memberId:guid}/borrowings", async (
                Guid memberId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetBorrowingsByMemberQuery(memberId), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            });

            return app;
        }
    }
}
