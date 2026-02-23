using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class EmailDomainSpec
{
    [Fact]
    public void WhenCreateWithInvalidDomain_ThenReturnsError()
    {
        var result = EmailDomain.Create("^aninvaliddomain^");

        result.Should().BeError(ErrorCode.Validation, Resources.EmailDomain_InvalidDomain);
    }

    [Fact]
    public void WhenCreateWithPersonalDomain_ThenReturnsDomain()
    {
        var result = EmailDomain.Create("personal.com");

        result.Should().BeSuccess();
        result.Value.Domain.Should().Be("personal.com");
        result.Value.Classification.Should().Be(EmailAddressClassification.Personal);
    }

    [Fact]
    public void WhenCreateWithCompanyDomain_ThenReturnsDomain()
    {
        var result = EmailDomain.Create("company.com");

        result.Should().BeSuccess();
        result.Value.Domain.Should().Be("company.com");
        result.Value.Classification.Should().Be(EmailAddressClassification.Company);
    }

    [Fact]
    public void WhenCreateWithSpecifiedDomain_ThenReturnsDomain()
    {
        var result = EmailDomain.Create("company.com", EmailAddressClassification.Personal);

        result.Should().BeSuccess();
        result.Value.Domain.Should().Be("company.com");
        result.Value.Classification.Should().Be(EmailAddressClassification.Personal);
    }
}