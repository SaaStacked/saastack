namespace WebsiteHost.Models;

public class IndexSpaPage
{
    public required string CSRFFieldName { get; set; }

    public required string CSRFHeaderToken { get; set; }

    public required string IsHostedOn { get; set; }

    public required bool IsJsAppBundled { get; set; }

    public bool IsTestingOnly { get; set; }

    public required string JsAppCssPath { get; set; }

    public required string JsAppJsPath { get; set; }
}