using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Common.Recording;
using Moq;
using WebsiteHost.Application;
using Xunit;

namespace WebsiteHost.UnitTests;

[Trait("Category", "Unit")]
public class RecordingApplicationSpec
{
    private readonly RecordingApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IRecorder> _recorder;

    public RecordingApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        _application = new RecordingApplication(_recorder.Object);
    }

    [Fact]
    public void WhenRecordCrashAsync_ThenRecords()
    {
        _application.RecordCrashAsync(_caller.Object, "amessage", CancellationToken.None);

        _recorder.Verify(rec => rec.Crash(It.IsAny<ICallContext>(), CrashLevel.Critical,
            It.Is<Exception>(ex =>
                ex.Message == Resources.RecordingApplication_RecordCrash_ExceptionMessage.Format("amessage")
            )));
    }

    [Fact]
    public void WhenRecordMeasurementAsyncWithEvent_ThenRecords()
    {
        _application.RecordMeasurementAsync(_caller.Object, "aneventname", null, new ClientDetails
        {
            IpAddress = "anipaddress",
            Referer = "areferer",
            UserAgent = "auseragent"
        }, CancellationToken.None);

        _recorder.Verify(rec => rec.Measure(It.IsAny<ICallContext>(), "aneventname",
            It.Is<Dictionary<string, object>>(dic =>
                dic.Count == 5
                && ((DateTime)dic[UsageConstants.Properties.Timestamp]).IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1))
                && (string)dic[UsageConstants.Properties.IpAddress] == "anipaddress"
                && (string)dic[UsageConstants.Properties.ReferredBy] == "areferer"
                && (string)dic[UsageConstants.Properties.UserAgent] == "auseragent"
                && (string)dic[UsageConstants.Properties.Component]
                == UsageConstants.Components.BackEndForFrontEndWebHost
            )));
    }

    [Fact]
    public void WhenRecordMeasurementAsyncWithAdditional_ThenRecords()
    {
        _application.RecordMeasurementAsync(_caller.Object, "aneventname", new Dictionary<string, object?>
        {
            { "aname", "avalue" }
        }, new ClientDetails
        {
            IpAddress = "anipaddress",
            Referer = "areferer",
            UserAgent = "auseragent"
        }, CancellationToken.None);

        _recorder.Verify(rec => rec.Measure(It.IsAny<ICallContext>(), "aneventname",
            It.Is<Dictionary<string, object>>(dic =>
                dic.Count == 6
                && (string)dic["aname"] == "avalue"
                && ((DateTime)dic[UsageConstants.Properties.Timestamp]).IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1))
                && (string)dic[UsageConstants.Properties.IpAddress] == "anipaddress"
                && (string)dic[UsageConstants.Properties.ReferredBy] == "areferer"
                && (string)dic[UsageConstants.Properties.UserAgent] == "auseragent"
                && (string)dic[UsageConstants.Properties.Component]
                == UsageConstants.Components.BackEndForFrontEndWebHost
            )));
    }

    [Fact]
    public void WhenRecordPageViewAsync_ThenRecords()
    {
        _application.RecordPageViewAsync(_caller.Object, "apath", new ClientDetails
        {
            IpAddress = "anipaddress",
            Referer = "areferer",
            UserAgent = "auseragent"
        }, CancellationToken.None);

        _recorder.Verify(rec => rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.Web.WebPageVisit,
            It.Is<Dictionary<string, object>>(dic =>
                dic.Count == 6
                && (string)dic[UsageConstants.Properties.Path] == "apath"
                && ((DateTime)dic[UsageConstants.Properties.Timestamp]).IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1))
                && (string)dic[UsageConstants.Properties.IpAddress] == "anipaddress"
                && (string)dic[UsageConstants.Properties.ReferredBy] == "areferer"
                && (string)dic[UsageConstants.Properties.UserAgent] == "auseragent"
                && (string)dic[UsageConstants.Properties.Component]
                == UsageConstants.Components.BackEndForFrontEndWebHost
            )));
    }

    [Fact]
    public void WhenRecordRecordTraceAsync_ThenRecords()
    {
        var args = new Dictionary<string, object?>
        {
            { "aname", "avalue" }
        };
        _application.RecordTraceAsync(_caller.Object, RecorderTraceLevel.Information, "amessage", args,
            CancellationToken.None);

        _recorder.Verify(rec => rec.TraceInformation(It.IsAny<ICallContext>(), "amessage",
            // ReSharper disable once StructuredMessageTemplateProblem
            It.Is<object[]>(a =>
                a.Length == 1
                && (string)a[0] == "avalue"
            )
        ));
    }

    [Fact]
    public void WhenRecordUsageAsyncWithMessage_ThenRecords()
    {
        _application.RecordUsageAsync(_caller.Object, "aneventname", null, new ClientDetails
        {
            IpAddress = "anipaddress",
            Referer = "areferer",
            UserAgent = "auseragent"
        }, CancellationToken.None);

        _recorder.Verify(rec => rec.TrackUsage(It.IsAny<ICallContext>(), "aneventname",
            It.Is<Dictionary<string, object>>(dic =>
                dic.Count == 5
                && ((DateTime)dic[UsageConstants.Properties.Timestamp]).IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1))
                && (string)dic[UsageConstants.Properties.IpAddress] == "anipaddress"
                && (string)dic[UsageConstants.Properties.ReferredBy] == "areferer"
                && (string)dic[UsageConstants.Properties.UserAgent] == "auseragent"
                && (string)dic[UsageConstants.Properties.Component]
                == UsageConstants.Components.BackEndForFrontEndWebHost
            )));
    }

    [Fact]
    public void WhenRecordUsageAsyncWithAdditional_ThenRecords()
    {
        _application.RecordUsageAsync(_caller.Object, "aneventname", new Dictionary<string, object?>
        {
            { "aname", "avalue" }
        }, new ClientDetails
        {
            IpAddress = "anipaddress",
            Referer = "areferer",
            UserAgent = "auseragent"
        }, CancellationToken.None);

        _recorder.Verify(rec => rec.TrackUsage(It.IsAny<ICallContext>(), "aneventname",
            It.Is<Dictionary<string, object>>(dic =>
                dic.Count == 6
                && (string)dic["aname"] == "avalue"
                && ((DateTime)dic[UsageConstants.Properties.Timestamp]).IsNear(DateTime.UtcNow, TimeSpan.FromMinutes(1))
                && (string)dic[UsageConstants.Properties.IpAddress] == "anipaddress"
                && (string)dic[UsageConstants.Properties.ReferredBy] == "areferer"
                && (string)dic[UsageConstants.Properties.UserAgent] == "auseragent"
                && (string)dic[UsageConstants.Properties.Component]
                == UsageConstants.Components.BackEndForFrontEndWebHost
            )));
    }
}