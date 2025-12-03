using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Organizations;
using FluentAssertions;
using Moq;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using OrganizationsInfrastructure.DomainServices;
using Xunit;

namespace OrganizationsInfrastructure.UnitTests.DomainServices;

[Trait("Category", "Unit")]
public class OrganizationEmailDomainServiceSpec
{
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IOrganizationRepository> _repository;
    private readonly OrganizationEmailDomainService _service;

    public OrganizationEmailDomainServiceSpec()
    {
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity _) => "anid".ToId());
        _repository = new Mock<IOrganizationRepository>();
        _repository.Setup(r => r.FindByEmailDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OrganizationRoot>.None);

        _service = new OrganizationEmailDomainService(_repository.Object);
    }

    [Fact]
    public async Task WhenEnsureUniqueAsyncAndNoOrganizationsFound_ThenReturnsTrue()
    {
        var result = await _service.EnsureUniqueAsync("aemaildomain", "anid".ToId(), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEnsureUniqueAsyncAndThisOrganizationFound_ThenReturnsTrue()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            new Mock<ITenantSettingService>().Object, _service, OrganizationOwnership.Shared, "acreatorid".ToId(),
            Optional<EmailAddress>.None, UserClassification.Person, DisplayName.Create("aname").Value,
            DatacenterLocations.Local).Value;
        _repository.Setup(r => r.FindByEmailDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org.ToOptional());

        var result = await _service.EnsureUniqueAsync("aemaildomain", "anid".ToId(), CancellationToken.None);

        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task WhenEnsureUniqueAsyncAndOtherOrganizationFound_ThenReturnsFalse()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            new Mock<ITenantSettingService>().Object, _service, OrganizationOwnership.Shared, "acreatorid".ToId(),
            Optional<EmailAddress>.None, UserClassification.Person, DisplayName.Create("aname").Value,
            DatacenterLocations.Local).Value;
        _repository.Setup(r => r.FindByEmailDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org.ToOptional());

        var result = await _service.EnsureUniqueAsync("aemaildomain", "anotherid".ToId(), CancellationToken.None);

        result.Should().BeFalse();
    }
}