using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

public class GetSharedOrganizationForCallerEmailDomainResponse : IWebResponse
{
    public required Organization Organization { get; set; }
}