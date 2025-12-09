#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the automatic population of default organization in tenanted requests
/// </summary>
[UsedImplicitly]
[Route("/testingonly/tenanted", OperationMethod.Get, AccessType.Anonymous, true)]
public class
    GetTenantedTestingOnlyRequest : TenantedRequest<GetTenantedTestingOnlyRequest, GetTenantedTestingOnlyResponse>
{
}
#endif