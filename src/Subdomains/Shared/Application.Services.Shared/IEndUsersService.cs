using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IEndUsersService
{
    Task<Result<EndUserWithMemberships, Error>> GetMembershipsPrivateAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> GetUserPrivateAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<SearchResults<MembershipWithUserProfile>, Error>> ListMembershipsForOrganizationAsync(
        ICallerContext caller,
        string organizationId, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<EndUser, Error>> RegisterMachinePrivateAsync(ICallerContext caller, string name,
        string? timezone, string? countryCode, CancellationToken cancellationToken);

    Task<Result<EndUserWithProfile, Error>> RegisterPersonPrivateAsync(ICallerContext caller, string? invitationToken,
        string emailAddress, string firstName, string? lastName, string? timezone, string? locale, string? countryCode,
        bool termsAndConditionsAccepted, CancellationToken cancellationToken);
}