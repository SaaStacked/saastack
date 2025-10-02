using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

public class GetOrganizationSettingsResponse : IWebResponse
{
    public required Organization Organization { get; set; }

    public Dictionary<string, string> Settings { get; set; } = new();
}