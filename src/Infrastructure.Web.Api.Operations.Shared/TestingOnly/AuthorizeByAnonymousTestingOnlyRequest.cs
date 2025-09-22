#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests access with no authorization (i.e., anonymous)
/// </summary>
[Route("/testingonly/authz/anonymous/get", OperationMethod.Get, AccessType.Anonymous, true)]
public class
    AuthorizeByAnonymousTestingOnlyRequest : WebRequest<AuthorizeByAnonymousTestingOnlyRequest,
    GetCallerTestingOnlyResponse>;
#endif