using Library.Api.Common.Filters;
using Library.Api.Common.Http;
using Library.Application.Contracts.Members;
using Library.Application.Members.Commands.CreateMember;
using Library.Application.Members.Queries.GetAllMembers;
using Library.Application.Members.Queries.GetMemberById;
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

            group.MapGet("/", async (ISender sender) =>
            {
                var members = await sender.Send(new GetAllMembersQuery());

                return Results.Ok(members);
            });

            group.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender) =>
            {
                var result = await sender.Send(new GetMemberByIdQuery(id));

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            });

            group.MapPost("/", async (
                CreateMemberRequest request,
                ISender sender) =>
            {
                var command = new CreateMemberCommand(
                    request.Name,
                    request.Email,
                    request.PhoneNumber);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Created($"/api/members/{result.Value.Id}", result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<CreateMemberRequest>>();

            return app;
        }
    }
}
