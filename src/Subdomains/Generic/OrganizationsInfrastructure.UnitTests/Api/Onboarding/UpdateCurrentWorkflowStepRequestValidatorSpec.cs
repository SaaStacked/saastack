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
public class UpdateCurrentWorkflowStepRequestValidatorSpec
{
    private readonly UpdateCurrentWorkflowStepRequest _dto;
    private readonly UpdateCurrentWorkflowStepRequestValidator _validator;

    public UpdateCurrentWorkflowStepRequestValidatorSpec()
    {
        _validator = new UpdateCurrentWorkflowStepRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new UpdateCurrentWorkflowStepRequest
        {
            Id = "anid",
            Values = new Dictionary<string, string> { { "akey", "avalue" } }
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
    public void WhenValuesIsNull_ThenThrows()
    {
        _dto.Values = null!;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.UpdateCurrentWorkflowStepRequestValidator_InvalidValues);
    }

    [Fact]
    public void WhenValuesIsEmpty_ThenThrows()
    {
        _dto.Values = new Dictionary<string, string>();

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.UpdateCurrentWorkflowStepRequestValidator_InvalidValues);
    }
}