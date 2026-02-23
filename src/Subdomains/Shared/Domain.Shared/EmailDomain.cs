using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Validations;
using JetBrains.Annotations;

namespace Domain.Shared;

public sealed class EmailDomain : ValueObjectBase<EmailDomain>
{
    private static readonly IReadOnlySet<string> PersonalEmailProviderDomains = new HashSet<string>
    {
        // Major free email services
        "gmail.com",
        "googlemail.com",
        "outlook.com",
        "hotmail.com",
        "live.com",
        "msn.com",
        "yahoo.com",
        "ymail.com",
        "aol.com",
        "icloud.com",
        "me.com",
        "mac.com",

        // International free email services
        "mail.com",
        "gmx.com",
        "gmx.net",
        "web.de",
        "mail.ru",
        "yandex.com",
        "yandex.ru",
        "qq.com",
        "163.com",
        "126.com",
        "sina.com",
        "sohu.com",
        "naver.com",
        "daum.net",
        "hanmail.net",
        "rediffmail.com",
        "proton.me",
        "protonmail.com",
        "pm.me",
        "protonmail.ch",
        "tutanota.com",
        "zoho.com",

        // Temporary/disposable email services
        "guerrillamail.com",
        "mailinator.com",
        "10minutemail.com",
        "tempmail.com",
        "throwaway.email",

        // Other common personal email domains
        "fastmail.com",
        "hushmail.com",
        "inbox.com",
        "email.com",
        "personal.com"
    };

    public static Result<EmailDomain, Error> Create(string domain)
    {
        if (domain.IsInvalidParameter(CommonValidations.EmailDomain, nameof(domain),
                Resources.EmailDomain_InvalidDomain, out var error))
        {
            return error;
        }

        var classification = PersonalEmailProviderDomains.ContainsIgnoreCase(domain)
            ? EmailAddressClassification.Personal
            : EmailAddressClassification.Company;

        return Create(domain, classification);
    }

    public static Result<EmailDomain, Error> Create(string domain, EmailAddressClassification classification)
    {
        if (domain.IsInvalidParameter(CommonValidations.EmailDomain, nameof(domain),
                Resources.EmailDomain_InvalidDomain, out var error))
        {
            return error;
        }

        return new EmailDomain(domain, classification);
    }

    private EmailDomain(string domain, EmailAddressClassification classification)
    {
        Domain = domain;
        Classification = classification;
    }

    public EmailAddressClassification Classification { get; }

    public string Domain { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<EmailDomain> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new EmailDomain(parts[0], parts[1].Value.ToEnumOrDefault(EmailAddressClassification.Personal));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Domain, Classification];
    }
}

public enum EmailAddressClassification
{
    Company = 0,
    Personal = 1
}