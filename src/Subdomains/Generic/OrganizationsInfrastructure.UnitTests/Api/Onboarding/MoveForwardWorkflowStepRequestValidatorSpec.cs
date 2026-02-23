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
public class MoveForwardWorkflowStepRequestValidatorSpec
{
    private readonly MoveForwardWorkflowStepRequest _dto;
    private readonly MoveForwardWorkflowStepRequestValidator _validator;

    public MoveForwardWorkflowStepRequestValidatorSpec()
    {
        _validator = new MoveForwardWorkflowStepRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new MoveForwardWorkflowStepRequest
        {
            Id = "anid",
            NextStepId = "astepid"
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

    [Fact]
    public void WhenNextStepIdIsInvalid_ThenThrows()
    {
        _dto.NextStepId = "aninvalidstepid^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.MoveToWorkflowStepRequestValidator_InvalidNextStepId);
    }

    [Fact]
    public void WhenNextStepIdIsNull_ThenSucceeds()
    {
        _dto.NextStepId = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenValuesIsNull_ThenSucceeds()
    {
        _dto.Values = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenValuesIsEmpty_ThenSucceeds()
    {
        _dto.Values = new Dictionary<string, string>();

        _validator.ValidateAndThrow(_dto);
    }
}