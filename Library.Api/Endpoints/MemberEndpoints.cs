using Library.Api.Common.Filters;
using Library.Application.Contracts.Mappings;
using Library.Application.Contracts.Members;
using Library.Application.Services;
using Library.Domain.Entities;

namespace Library.Api.Endpoints
{
    public static class MemberEndpoints
    {
        public static IEndpointRouteBuilder MapMemberEndpoints(
            this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/members")
                .WithTags("Members");

            group.MapGet("/", async (MemberService service) =>
            {
                var members = await service.GetAllAsync();

                return Results.Ok(
                    members.Select(m => m.ToResponse()));
            });

            group.MapGet("/{id:int}", async (
                int id,
                MemberService service) =>
            {
                var member = await service.GetByIdAsync(id);

                return Results.Ok(member.ToResponse());
            });

            group.MapPost("/", async (
                CreateMemberRequest request,
                MemberService service) =>
            {
                var member = new Member(
                    request.Name,
                    request.Email,
                    request.PhoneNumber);

                await service.CreateAsync(member);

                return Results.Created(
                    $"/api/members/{member.Id}",
                    member.ToResponse());
            })
            .AddEndpointFilter<ValidationFilter<CreateMemberRequest>>();

            return app;
        }
    }
}
