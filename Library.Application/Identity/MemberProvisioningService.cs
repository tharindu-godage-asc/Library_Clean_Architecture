using Library.Application.Interfaces;
using Library.Domain.Entities;

namespace Library.Application.Identity;

public sealed class MemberProvisioningService : IMemberProvisioningService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MemberProvisioningService(IMemberRepository memberRepository, IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> EnsureMemberAsync(
        string keycloakId,
        string email,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByKeycloakIdAsync(keycloakId, cancellationToken);
        if (member is not null)
        {
            return member.Id;
        }

        // An existing member (created before Keycloak existed) registering fresh in Keycloak
        // gets linked here instead of duplicated — see docs/keycloak-authserver-phase3-member-provisioning.md.
        member = await _memberRepository.GetByEmailAsync(email, cancellationToken);
        if (member is not null)
        {
            member.LinkKeycloakId(keycloakId);
            _memberRepository.Update(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return member.Id;
        }

        var createResult = Member.CreateFromKeycloak(keycloakId, name ?? email, email);
        if (createResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to provision a Member for Keycloak identity '{keycloakId}': {createResult.Error.Message}");
        }

        member = createResult.Value;
        await _memberRepository.AddAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return member.Id;
    }
}
