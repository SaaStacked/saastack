namespace WebsiteHost.ApplicationServices;

/// <summary>
///     Defines a bundler for the JavaScript Application
/// </summary>
public interface IJsAppBundler
{
    /// <summary>
    ///     Retrieves the bundle options
    /// </summary>
    JsAppBundleOptions GetBundleOptions();
}

public class JsAppBundleOptions
{
    public required string CssPath { get; init; }

    public required bool IsBundled { get; init; }

    public required string JsPath { get; init; }
}