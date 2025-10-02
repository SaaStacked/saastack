using Application.Resources.Shared;
using Common.Extensions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using JetBrains.Annotations;

namespace WebsiteHost.Api.Recording;

[UsedImplicitly]
public class RecordTraceRequestValidator : AbstractValidator<RecordTraceRequest>
{
    public RecordTraceRequestValidator()
    {
        RuleFor(req => req.Level)
            .NotNull()
            .IsEnumName(typeof(RecorderTraceLevel), false)
            .WithMessage(Resources.RecordTraceRequestValidator_InvalidLevel);
        RuleFor(req => req.MessageTemplate)
            .NotEmpty()
            .WithMessage(Resources.RecordTraceRequestValidator_InvalidMessageTemplate);
        RuleForEach(req => req.Arguments)
            .NotNull()
            .Must(pair => pair.Value.Exists())
            .WithMessage(Resources.RecordTraceRequestValidator_InvalidMessageArgument);
    }
}