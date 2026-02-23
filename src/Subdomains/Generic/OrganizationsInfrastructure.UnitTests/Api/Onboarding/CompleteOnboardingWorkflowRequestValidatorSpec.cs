using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;
using OrganizationsInfrastructure.Api.Onboarding;
using UnitTesting.Common.Validation;
using Xunit;

namespace OrganizationsInfrastructure.UnitTests.Api.Onboarding;

[Trait("Category", "Unit")]
public class CompleteOnboardingWorkflowRequestValidatorSpec
{
    private readonly CompleteOnboardingWorkflowRequest _dto;
    private readonly CompleteOnboardingWorkflowRequestValidator _validator;

    public CompleteOnboardingWorkflowRequestValidatorSpec()
    {
        _validator = new CompleteOnboardingWorkflowRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new CompleteOnboardingWorkflowRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenIdIsInvalid_ThenThrows()
    {
        _dto.Id = "aninvalidid^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(CommonValidationResources.AnyValidator_InvalidId);
    }
}