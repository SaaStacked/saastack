#if TESTINGONLY
using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

#pragma warning disable SAASDDD043
public sealed class OnboardingReset : DomainEvent
#pragma warning restore SAASDDD043
{
    public OnboardingReset(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public OnboardingReset()
    {
    }
}
#endif