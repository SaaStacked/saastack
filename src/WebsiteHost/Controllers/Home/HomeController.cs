using Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace WebsiteHost.Controllers.Home;

public class HomeController : CSRFController
{
    public HomeController(IRecorder recorder, IHostEnvironment hostEnvironment, IHttpClientFactory httpClientFactory,
        CSRFMiddleware.ICSRFService csrfService) :
        base(recorder, hostEnvironment, httpClientFactory, csrfService)
    {
    }

    [HttpGet("error")]
    public IActionResult Error()
    {
        return Problem();
    }

    public IActionResult Index()
    {
        return CSRFResult();
    }
}