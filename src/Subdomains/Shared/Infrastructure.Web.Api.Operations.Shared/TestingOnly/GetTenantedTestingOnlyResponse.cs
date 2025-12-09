#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[UsedImplicitly]
public class GetTenantedTestingOnlyResponse : IWebResponse
{
    public string? OrganizationId { get; set; }
}
#endif