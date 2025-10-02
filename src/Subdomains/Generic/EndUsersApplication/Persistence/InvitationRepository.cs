using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Shared;
using EndUsersDomain;

namespace EndUsersApplication.Persistence;

public interface IInvitationRepository : IApplicationRepository
{
    Task<Result<Optional<EndUserRoot>, Error>> FindInvitedGuestByEmailAddressAsync(EmailAddress emailAddress,
        CancellationToken cancellationToken);

    Task<Result<Optional<EndUserRoot>, Error>> FindInvitedGuestByTokenAsync(string token,
        CancellationToken cancellationToken);

    Task<Result<EndUserRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<EndUserRoot, Error>> SaveAsync(EndUserRoot user, CancellationToken cancellationToken);
}