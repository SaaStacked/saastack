#if HOSTEDONAWS
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces.Services;
using Infrastructure.External.Persistence.AWS;
using Infrastructure.External.Persistence.AWS.Extensions;

namespace Infrastructure.Common.Recording;

/// <summary>
///     CloudWatch can only record a single counter metric
/// </summary>
public class AWSCloudWatchMetricReporter : IMetricReporter
{
    private readonly AmazonCloudWatchClient _client;

    public AWSCloudWatchMetricReporter(IDependencyContainer container)
    {
        var settings = container.GetRequiredServiceForPlatform<IConfigurationSettings>();
        var (credentials, regionEndpoint) = settings.GetConnection();
        if (regionEndpoint.Exists())
        {
            _client = new AmazonCloudWatchClient(credentials, regionEndpoint);
        }
        else
        {
            _client = new AmazonCloudWatchClient(credentials,
                new AmazonCloudWatchConfig { ServiceURL = AWSConstants.LocalStackServiceUrl });
        }
    }

    public void Measure(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        _client.PutMetricDataAsync(new PutMetricDataRequest
        {
            Namespace = "SaaStack",
            MetricData =
            [
                new MetricDatum
                {
                    MetricName = eventName,
                    Value = 1
                }
            ]
        });
    }
}
#endif