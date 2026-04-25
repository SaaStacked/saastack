using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a <see cref="IUserNotificationsService" /> that delivers notifications via asynchronous email delivery
///     using <see cref="IEmailSchedulingService" /> and via asynchronous SMS text message delivery using
///     <see cref="ISmsSchedulingService" />
/// </summary>
public partial class MessageUserNotificationsService : IUserNotificationsService
{
    private const string ProductNameSettingName = "ApplicationServices:EmailNotifications:SenderProductName";
    private const string SenderDisplayNameSettingName = "ApplicationServices:EmailNotifications:SenderDisplayName";
    private const string SenderEmailAddressSettingName = "ApplicationServices:EmailNotifications:SenderEmailAddress";
    private readonly BrandingInfo _brandingInfo;
    private readonly IEmailSchedulingService _emailSchedulingService;
    private readonly string _senderEmailAddress;
    private readonly string _senderName;
    private readonly ISmsSchedulingService _smsSchedulingService;
    private readonly IWebsiteUiService _websiteUiService;

    public MessageUserNotificationsService(IConfigurationSettings settings,
        IWebsiteUiService websiteUiService, IEmailSchedulingService emailSchedulingService,
        ISmsSchedulingService smsSchedulingService)
    {
        _websiteUiService = websiteUiService;
        _emailSchedulingService = emailSchedulingService;
        _smsSchedulingService = smsSchedulingService;
        _brandingInfo =
            new BrandingInfo(settings.Platform.GetString(ProductNameSettingName,
                nameof(MessageUserNotificationsService)));
        _senderEmailAddress =
            settings.Platform.GetString(SenderEmailAddressSettingName, nameof(MessageUserNotificationsService));
        _senderName =
            settings.Platform.GetString(SenderDisplayNameSettingName, nameof(MessageUserNotificationsService));
    }

    public async Task<Result<Error>> NotifyGuestInvitationToPlatformAsync(ICallerContext caller, string token,
        string inviteeEmailAddress, string inviteeName, string inviterName, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        var link = _websiteUiService.CreateRegistrationPageUrl(token);
        var content = GuestInvitationToPlatform(_brandingInfo, inviterName, link);
        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = content.Subject,
            Body = content.HtmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = inviteeEmailAddress,
            ToDisplayName = inviteeName,
            Tags = tags.Exists()
                ? [..tags]
                : null
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordMfaOobEmailAsync(ICallerContext caller, string emailAddress,
        string code,
        IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        var link = _websiteUiService.ConstructPasswordMfaOobConfirmationPageUrl(code);
        var content = PasswordMfaOobEmail(_brandingInfo, code, link);
        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = content.Subject,
            Body = content.HtmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = emailAddress,
            Tags = tags.Exists()
                ? [..tags]
                : null
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordMfaOobSmsAsync(ICallerContext caller, string phoneNumber,
        string code,
        IReadOnlyList<string>? tags, CancellationToken cancellationToken)
    {
        var content = PasswordMfaOobSms(_brandingInfo, code);
        return await _smsSchedulingService.ScheduleSms(caller, new SmsText
        {
            Body = content,
            To = phoneNumber,
            Tags = tags.Exists()
                ? [..tags]
                : null
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordRegistrationConfirmationAsync(ICallerContext caller,
        string emailAddress, string name, string token, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        var link = _websiteUiService.ConstructPasswordRegistrationConfirmationPageUrl(token);
        var content = PasswordRegistrationConfirmation(_brandingInfo, name, link);

        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = content.Subject,
            Body = content.HtmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = name,
            Tags = tags.Exists()
                ? [..tags]
                : null
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordRegistrationRepeatCourtesyAsync(ICallerContext caller, string userId,
        string emailAddress, string name, string? timezone, string? countryCode, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        var content = PasswordRegistrationRepeatCourtesy(_brandingInfo, name, emailAddress);
        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = content.Subject,
            Body = content.HtmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = name,
            Tags = tags.Exists()
                ? [..tags]
                : null
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordResetInitiatedAsync(ICallerContext caller, string name,
        string emailAddress, string token, IReadOnlyList<string>? tags, CancellationToken cancellationToken)
    {
        var link = _websiteUiService.ConstructPasswordResetConfirmationPageUrl(token);
        var content = PasswordResetInitiated(_brandingInfo, name, link);
        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = content.Subject,
            Body = content.HtmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = name,
            Tags = tags.Exists()
                ? [..tags]
                : null
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifyPasswordResetUnknownUserCourtesyAsync(ICallerContext caller,
        string emailAddress, IReadOnlyList<string>? tags, CancellationToken cancellationToken)
    {
        var content = PasswordResetUnknownUserCourtesy(_brandingInfo);
        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = content.Subject,
            Body = content.HtmlBody,
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = emailAddress,
            Tags = tags.Exists()
                ? [..tags]
                : null
        }, cancellationToken);
    }

    public async Task<Result<Error>> NotifySubscriptionTrialEventEmailAsync(ICallerContext caller, string emailAddress,
        string name, string companyName, string eventId, IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        if (!TrialSubscriptionEmailMessages.TryGetValue(eventId, out var message))
        {
            throw new InvalidOperationException(
                $"Trial Subscription Email Message for '{eventId}' has not been defined");
        }

        var trialInfo = new TrialInfo(name, companyName);

        return await _emailSchedulingService.ScheduleHtmlEmail(caller, new HtmlEmail
        {
            Subject = message.Subject(_brandingInfo, trialInfo),
            Body = message.HtmlBody(_brandingInfo, trialInfo),
            FromEmailAddress = _senderEmailAddress,
            FromDisplayName = _senderName,
            ToEmailAddress = emailAddress,
            ToDisplayName = name,
            Tags = tags.Exists()
                ? [..tags]
                : null
        }, cancellationToken);
    }
}