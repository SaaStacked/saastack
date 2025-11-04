using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Common.Extensions;
using JetBrains.Annotations;

namespace WebsiteHost.ApplicationServices;

/// <summary>
///     Provides bundle information for the JavaScript Application created by Vite
/// </summary>
public class ViteJsAppBundler : IJsAppBundler
{
    private const string CssAppEntryPoint = "src/main.css";
    private const string JsAppEntryPoint = "src/main.tsx";
    private const int ViteDevServerPort = 5173;
    private static readonly string BundlerOutputLocation = Path.Combine("ClientApp", "jsapp.build.json");
    private static readonly string ViteDevServer = $"http://localhost:{ViteDevServerPort}";
    private static readonly string ViteDevServerJsPath = $"{ViteDevServer}/{JsAppEntryPoint}";
    private static readonly string ViteDevServerCssPath = $"{ViteDevServer}/{CssAppEntryPoint}";

    private readonly IHostEnvironment _hostEnvironment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRecorder _recorder;

    public ViteJsAppBundler(IRecorder recorder, IHostEnvironment hostEnvironment, IHttpClientFactory httpClientFactory)
    {
        _recorder = recorder;
        _hostEnvironment = hostEnvironment;
        _httpClientFactory = httpClientFactory;
    }

    public JsAppBundleOptions GetBundleOptions()
    {
        if (_hostEnvironment.IsDevelopment()
            && IsViteDevServerRunning())
        {
            return new JsAppBundleOptions
            {
                JsPath = ViteDevServerJsPath,
                CssPath = ViteDevServerCssPath,
                IsBundled = false
            };
        }

        var jsAppData = GetJsAppBuildPath();
        var cssPath = jsAppData.Main?.Css;
        if (cssPath.HasValue())
        {
            return new JsAppBundleOptions
            {
                JsPath = $"/{jsAppData.Main!.Js!}",
                CssPath = $"/{jsAppData.Main!.Css!}",
                IsBundled = true
            };
        }

        throw new InvalidOperationException(
            Resources.ViteBundler_InvalidBundle.Format(BundlerOutputLocation));
    }

    private WebPackOutputJsonData GetJsAppBuildPath()
    {
        var basePath = _hostEnvironment.ContentRootPath;
        var outputFilePath = Path.Combine(basePath, BundlerOutputLocation);

        using var outputContent = File.OpenText(outputFilePath);

        try
        {
            return JsonSerializer.Deserialize<WebPackOutputJsonData>(outputContent.BaseStream)!;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(
                Resources.ViteBundler_InvalidBundle.Format(BundlerOutputLocation));
        }
    }

    private bool IsViteDevServerRunning()
    {
        using var httpClient = _httpClientFactory.CreateClient("ViteDevServer");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, ViteDevServer);
            httpClient.Send(request);
            return true;
        }
        catch
        {
            if (_hostEnvironment.IsDevelopment())
            {
                _recorder.TraceWarning(null, $"Vite Dev Server was not found running at: {ViteDevServer}");
            }

            return false;
        }
    }
}

[UsedImplicitly]
public class WebPackOutputJsonData
{
    [JsonPropertyName("main")] public Bundle? Main { get; set; }
}

[UsedImplicitly]
public class Bundle
{
    [JsonPropertyName("css")] public string? Css { get; set; }

    [JsonPropertyName("js")] public string? Js { get; set; }
}