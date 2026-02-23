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
public class MoveBackWorkflowStepRequestValidatorSpec
{
    private readonly MoveBackWorkflowStepRequest _dto;
    private readonly MoveBackWorkflowStepRequestValidator _validator;

    public MoveBackWorkflowStepRequestValidatorSpec()
    {
        _validator = new MoveBackWorkflowStepRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new MoveBackWorkflowStepRequest
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