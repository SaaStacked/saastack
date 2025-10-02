using Common;
using Domain.Events.Shared.EndUsers;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using UserProfilesApplication;
using ImageDeleted = Domain.Events.Shared.Images.Deleted;

namespace UserProfilesInfrastructure.Notifications;

public class NotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IUserProfilesApplication _userProfilesApplication;

    public NotificationConsumer(ICallerContextFactory callerContextFactory,
        IUserProfilesApplication userProfilesApplication)
    {
        _callerContextFactory = callerContextFactory;
        _userProfilesApplication = userProfilesApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Registered registered:
                return await _userProfilesApplication.HandleEndUserRegisteredAsync(_callerContextFactory.Create(),
                    registered, cancellationToken);

            case DefaultMembershipChanged changed:
                return await _userProfilesApplication.HandleEndUserDefaultMembershipChangedAsync(
                    _callerContextFactory.Create(),
                    changed, cancellationToken);

            case ImageDeleted deleted:
                return await _userProfilesApplication.HandleImageDeletedAsync(_callerContextFactory.Create(),
                    deleted, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}