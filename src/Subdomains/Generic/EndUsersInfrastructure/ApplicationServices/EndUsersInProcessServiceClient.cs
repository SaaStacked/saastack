using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using EndUsersApplication;

namespace EndUsersInfrastructure.ApplicationServices;

/// <summary>
///     Provides an in-process service client to be used to make cross-domain calls,
///     when the EndUsers subdomain is deployed in the same host as the consumer of this service
/// </summary>
public class EndUsersInProcessServiceClient : IEndUsersService
{
    private readonly IEndUsersApplication _endUsersApplication;

    public EndUsersInProcessServiceClient(IEndUsersApplication endUsersApplication)
    {
        _endUsersApplication = endUsersApplication;
    }

    public async Task<Result<EndUserWithMemberships, Error>> GetMembershipsPrivateAsync(ICallerContext caller,
        string id, CancellationToken cancellationToken)
    {
        return await _endUsersApplication.GetMembershipsAsync(caller, id, cancellationToken);
    }

    public async Task<Result<EndUser, Error>> GetUserPrivateAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await _endUsersApplication.GetUserAsync(caller, id, cancellationToken);
    }

    public async Task<Result<SearchResults<MembershipWithUserProfile>, Error>> ListMembershipsForOrganizationAsync(
        ICallerContext caller, string organizationId, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken)
    {
        return await _endUsersApplication.ListMembershipsForOrganizationAsync(caller, organizationId, searchOptions,
            getOptions, cancellationToken);
    }

    public async Task<Result<EndUser, Error>> RegisterMachinePrivateAsync(ICallerContext caller, string name,
        string? timezone, string? countryCode,
        CancellationToken cancellationToken)
    {
        return await _endUsersApplication.RegisterMachineAsync(caller, name, timezone, countryCode, cancellationToken);
    }

    public async Task<Result<EndUserWithProfile, Error>> RegisterPersonPrivateAsync(ICallerContext caller,
        string? invitationToken, string emailAddress, string firstName, string? lastName, string? timezone,
        string? locale, string? countryCode, bool termsAndConditionsAccepted, CancellationToken cancellationToken)
    {
        return await _endUsersApplication.RegisterPersonAsync(caller, invitationToken, emailAddress, firstName,
            lastName, timezone, locale, countryCode, termsAndConditionsAccepted, cancellationToken);
    }
}