namespace Infrastructure.Shared.ApplicationServices;

public partial class MessageUserNotificationsService
{
    // EXTEND: Add your other scheduled event email here, to match those defined in the BillingProviderCapabilities
    private static readonly Dictionary<string, SubscriptionTrialEventEmailMessage> TrialSubscriptionEmailMessages =
        new()
        {
            {
                "welcome", new SubscriptionTrialEventEmailMessage(
                    (branding, trial) => $"Welcome to {branding.ProductName} — {trial.CompanyName}'s trial has started",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p>You're in. <strong>{trial.CompanyName}'s</strong> free 14-day trial of {branding.ProductName} has started — no payment needed.</p>
                                          <p>Use the next two weeks to explore the platform, set up your critical risk controls, and see what verified visibility actually looks like in practice.</p>
                                          <p>You can add a payment method at any time — your access will continue seamlessly when the trial ends.</p>
                                          <p><em>This is an automated email, but there's a real person behind it. If you have questions, just reply.</em></p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            },
            {
                "gettingstarted", new SubscriptionTrialEventEmailMessage(
                    (branding, trial) =>
                        $"A few days in — making the most of {branding.ProductName} for {trial.CompanyName}",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p>You've been in {branding.ProductName} for a few days now — good start.</p>
                                          <p>If you haven't already, it's worth spending time on [key feature / onboarding step]. Most people find that's where it clicks. Our <a href="#">documentation and tutorials</a> will help you get there faster.</p>
                                          <p>11 days left in <strong>{trial.CompanyName}'s</strong> trial.</p>
                                          <p><em>This is an automated email, but if you'd like a hand, just reply — we're here.</em></p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            },
            {
                "midtrial", new SubscriptionTrialEventEmailMessage(
                    (branding, trial) =>
                        $"Halfway through {trial.CompanyName}'s {branding.ProductName} trial — how's it going?",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p>You're at the halfway mark of your {branding.ProductName} trial. We hope it's been useful.</p>
                                          <p>If you've hit a question, want a walkthrough of a specific feature, or just want to talk through whether {branding.ProductName} is the right fit — reply to this email. We'd genuinely like to hear how it's going.</p>
                                          <p>7 days left in <strong>{trial.CompanyName}'s</strong> trial.</p>
                                          <p><em>This is an automated email, but replying goes straight to a real person.</em></p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            },
            {
                "pretrialpaymentreminder", new SubscriptionTrialEventEmailMessage(
                    (branding, trial) =>
                        $"4 days left on {trial.CompanyName}'s {branding.ProductName} trial — add a payment method to keep your access",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p><strong>{trial.CompanyName}'s</strong> {branding.ProductName} trial ends in 4 days.</p>
                                          <p>To keep your access running without interruption, <a href="#">add a payment method now</a>. You won't be charged until the trial ends.</p>
                                          <p>If you don't add a payment method before the trial closes, your account will move to our limited free tier automatically.</p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            },
            {
                "urgentreminder", new SubscriptionTrialEventEmailMessage(
                    (branding, trial) => $"2 days left on {trial.CompanyName}'s {branding.ProductName} trial",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p><strong>{trial.CompanyName}'s</strong> {branding.ProductName} trial ends in 2 days.</p>
                                          <p><a href="#">Add a payment method now</a> to hold on to full access when the trial ends.</p>
                                          <p>Without a payment method, access to paid features will stop when the trial closes. Questions? Reply to this email.</p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            },
            {
                "finalreminder", new SubscriptionTrialEventEmailMessage(
                    (branding, trial) => $"{trial.CompanyName}'s {branding.ProductName} trial ends today",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p>Last chance — <strong>{trial.CompanyName}'s</strong> {branding.ProductName} trial ends today.</p>
                                          <p><a href="#">Add a payment method now</a> to avoid any break in access.</p>
                                          <p>If we don't receive payment today, we'll move your account to our limited free tier at the end of the day.</p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            },
            {
                "trialexpired", new SubscriptionTrialEventEmailMessage(
                    (branding, trial) => $"{trial.CompanyName}'s {branding.ProductName} trial has ended",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p><strong>{trial.CompanyName}'s</strong> {branding.ProductName} trial has ended and we've moved your account to our free tier.</p>
                                          <p>You won't have access to paid features, but your data is safe and waiting for you.</p>
                                          <p><a href="#">Upgrade now</a> to restore full access for you and your team. If you have questions or want to talk through next steps, just reply to this email.</p>
                                          <p><em>This is an automated email, but replying goes straight to a real person.</em></p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            },
            {
                "posttrialpaymentreminder1", new SubscriptionTrialEventEmailMessage(
                    (_, trial) => $"We'd love to have {trial.CompanyName} back",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p><strong>{trial.CompanyName}'s</strong> {branding.ProductName} trial ended a few days ago and we noticed you haven't upgraded yet.</p>
                                          <p>If something got in the way, or you'd like to talk through whether {branding.ProductName} is the right fit, just reply to this email. We're keen to hear about your experience rather than have you drift away.</p>
                                          <p>Your data is safe and your account is here whenever you're ready. <a href="#">Upgrade now</a> to restore full access.</p>
                                          <p><em>This is an automated email, but replying goes straight to a real person.</em></p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            },
            {
                "posttrialpaymentreminder2", new SubscriptionTrialEventEmailMessage(
                    (branding, _) => $"One last note from the {branding.ProductName} team",
                    (branding, trial) => $"""
                                          <p>Hi {trial.PersonFirstName},</p>
                                          <p>This is our last note about <strong>{trial.CompanyName}'s</strong> {branding.ProductName} trial.</p>
                                          <p>If there's something that stopped you getting started — a question, a concern, a feature you couldn't find — we'd genuinely like to know. Just reply.</p>
                                          <p>Your account and data are here if you decide to come back. <a href="#">Upgrade at any time</a> to restore full access.</p>
                                          <p><em>This is an automated email, but replying goes straight to a real person.</em></p>
                                          <p>— The {branding.ProductName} team</p>
                                          """)
            }
        };

    private static (string Subject, string HtmlBody) GuestInvitationToPlatform(BrandingInfo branding,
        string inviterName,
        string link)
    {
        return ($"You've been invited to {branding.ProductName}",
            $"""
             <p>Hi,</p>
             <p>{inviterName} has invited you to join them on {branding.ProductName}.</p>
             <p><a href="{link}">Sign up here</a> to get started.</p>
             <p><em>This is an automated email, but if you have questions, just reply — there's a real person here.</em></p>
             <p>— The {branding.ProductName} team</p>
             """);
    }

    private static (string Subject, string HtmlBody) PasswordMfaOobEmail(BrandingInfo branding, string code,
        string link)
    {
        return ($"Your {branding.ProductName} sign-in code",
            $"""
             <p>Here's your sign-in code for {branding.ProductName}:</p>
             <p><span style="font-weight: bold;font-size: x-large">{code}</span></p>
             <p><a href="{link}">Complete sign-in</a></p>
             <p><em>This is an automated email. If you didn't request this code, you can safely ignore it.</em></p>
             <p>— The {branding.ProductName} team</p>
             """);
    }

    private static string PasswordMfaOobSms(BrandingInfo branding, string code)
    {
        return
            $"""
             Your {branding.ProductName} sign-in code is {code}.
             """;
    }

    private static (string Subject, string HtmlBody) PasswordRegistrationConfirmation(BrandingInfo branding,
        string name,
        string link)
    {
        return ($"Confirm your email to get started with {branding.ProductName}",
            $"""
             <p>Hi {name},</p>
             <p>Thanks for signing up to {branding.ProductName}. One last step — <a href="{link}">confirm your email address</a> and you're in.</p>
             <p><em>This is an automated email. If you didn't sign up for {branding.ProductName}, you can ignore this.</em></p>
             <p>— The {branding.ProductName} team</p>
             """);
    }

    private static (string Subject, string HtmlBody) PasswordRegistrationRepeatCourtesy(BrandingInfo branding,
        string name,
        string emailAddress)
    {
        return ($"Someone tried to register your email at {branding.ProductName}",
            $"""
             <p>Hi {name},</p>
             <p>We received a request to register '{emailAddress}' at {branding.ProductName} — but that address is already registered to an account.</p>
             <p>We blocked the attempt, so nothing has changed on your end. Your account is safe.</p>
             <p>If this was you trying to sign up again, you might just need to <a href="#">reset your password</a> instead. If it wasn't you, no action is needed — we've already stopped it.</p>
             <p><em>This is an automated email. If something seems off, just reply and we'll take a look.</em></p>
             <p>— The {branding.ProductName} team</p>
             """);
    }

    private static (string Subject, string HtmlBody) PasswordResetInitiated(BrandingInfo info, string name, string link)
    {
        return ($"Reset your {info.ProductName} password",
            $$"""
              <p>Hi {{name}},</p>
              <p>We received a request to reset your {{info.ProductName}} password. <a href="{{link}}">Reset it here.</a></p>
              <p>If you didn't request this, please contact us immediately by replying to this email.</p>
              <p><em>This is an automated email, but replying goes straight to a real person.</em></p>
              <p>— The {branding.ProductName} team</p>
              """);
    }

    private static (string Subject, string HtmlBody) PasswordResetUnknownUserCourtesy(BrandingInfo branding)
    {
        return ($"Password reset attempt at {branding.ProductName}",
            $"""
             <p>Hi,</p>
             <p>We received a request to reset a password at {branding.ProductName} using your email address — but there's no account registered to it.</p>
             <p>We blocked the attempt. You don't need to do anything.</p>
             <p>If this seems suspicious, just reply to this email and we'll look into it.</p>
             <p><em>This is an automated email, but replying goes straight to a real person.</em></p>
             <p>— The {branding.ProductName} team</p>
             """);
    }

    /// <summary>
    ///     Defines the branding info for use in messaging
    /// </summary>
    public record TrialInfo(string PersonFirstName, string CompanyName);

    /// <summary>
    ///     Defines the branding info for use in messaging
    /// </summary>
    public record BrandingInfo(string ProductName);

    /// <summary>
    ///     Defines a subscription trial email message
    /// </summary>
    public record SubscriptionTrialEventEmailMessage(
        Func<BrandingInfo, TrialInfo, string> Subject,
        Func<BrandingInfo, TrialInfo, string> HtmlBody);
}