using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a registration of middleware
/// </summary>
public class MiddlewareRegistration
{
    public MiddlewareRegistration(int priority, Func<WebApplication, Result> action)
    {
        Priority = priority;
        Action = action;
    }

    public Func<WebApplication, Result> Action { get; }

    public int Priority { get; set; }

    public void Register(WebApplication app)
    {
        var result = Action(app);
        if (result.IsReporting)
        {
            app.Logger.LogInformation(result.Message, result.MessageArgs);
        }
    }

    /// <summary>
    ///     Provides the result of registering the middleware
    /// </summary>
    public class Result
    {
        public static readonly Result Ignore = new(false, null!);

        private Result(bool isReporting, string? message, params object[] args)
        {
            IsReporting = isReporting;
            Message = message;
            MessageArgs = args;
        }

        public bool IsReporting { get; }

        public string? Message { get; }

        public object[] MessageArgs { get; }

        public static Result Report(string message, params object[] args)
        {
            return new Result(true, message, args);
        }
    }
}
