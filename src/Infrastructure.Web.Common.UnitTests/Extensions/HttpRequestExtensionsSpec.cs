using System.Text;
using Common;
using FluentAssertions;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class HttpRequestExtensionsSpec
{
    [Fact]
    public void WhenSetRequestIdAndCallIdEmpty_ThenNoSetHeader()
    {
        var context = Mock.Of<ICallContext>(cc => cc.CallId == string.Empty);
        var message = new HttpRequestMessage();

        message.SetRequestId(context);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetRequestIdAndHeaderAlreadySet_ThenNoSetHeader()
    {
        var context = Mock.Of<ICallContext>(cc => cc.CallId == "acallid");
        var message = new HttpRequestMessage
        {
            Headers = { { HttpConstants.Headers.RequestId, "arequestid" } }
        };

        message.SetRequestId(context);

        message.Headers.GetValues(HttpConstants.Headers.RequestId).Should().OnlyContain(hdr => hdr == "arequestid");
    }

    [Fact]
    public void WhenSetRequestIdAndHeaderNotSet_ThenSetsHeader()
    {
        var context = Mock.Of<ICallContext>(cc => cc.CallId == "acallid");
        var message = new HttpRequestMessage();

        message.SetRequestId(context);

        message.Headers.GetValues(HttpConstants.Headers.RequestId).Should().OnlyContain(hdr => hdr == "acallid");
    }

    [Fact]
    public void WhenSetJWTBearerTokenAndNoTokenValue_ThenDoesNothing()
    {
        var message = new HttpRequestMessage();

        message.SetJWTBearerToken(string.Empty);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetJWTBearerToken_ThenSetsBearerAuthorization()
    {
        var message = new HttpRequestMessage();

        message.SetJWTBearerToken("atoken");

        message.Headers.GetValues(HttpConstants.Headers.Authorization).Should()
            .OnlyContain(hdr => hdr == "Bearer atoken");
    }

    [Fact]
    public void WhenSetAPIKeyAndNoKeyValue_ThenDoesNothing()
    {
        var message = new HttpRequestMessage();

        message.SetAPIKey(string.Empty);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetAPIKey_ThenSetsBasicAuthorization()
    {
        var message = new HttpRequestMessage();

        message.SetAPIKey("anapikey");

        var base64Credential = Convert.ToBase64String(Encoding.UTF8.GetBytes("anapikey:"));
        message.Headers.GetValues(HttpConstants.Headers.Authorization).Should()
            .OnlyContain(hdr => hdr == $"Basic {base64Credential}");
    }

    [Fact]
    public void WhenRewindBodAndCanSeek_ThenRewindsStream()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("avalue"));
        stream.Seek(0, SeekOrigin.End);
        var httpRequest = Mock.Of<HttpRequest>(req => req.Body == stream);

        httpRequest.RewindBody();

        stream.Position.Should().Be(0);
    }

    [Fact]
    public void WhenGetTokenAuthAndNoAuthorizationHeader_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());

        var result = httpRequest.Object.GetTokenAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetTokenAuthAndNoAuthorizationHeaderValues_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, new StringValues(new string[] { }) }
        });

        var result = httpRequest.Object.GetTokenAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetTokenAuthAndAuthorizationHeaderValuesContainsNoBearer_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, new StringValues(new[] { "avalue1", "avalue2" }) }
        });

        var result = httpRequest.Object.GetTokenAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetTokenAuthAndAuthorizationHeaderValuesContainsBearer_ThenReturnsToken()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, new StringValues(new[] { "avalue1", "Bearer avalue2" }) }
        });

        var result = httpRequest.Object.GetTokenAuth();

        result.Should().BeSome("avalue2");
    }

    [Fact]
    public void WhenGetBasicAuthAndNoAuthorizationHeader_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeNone();
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderNotBasic_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeNone();
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasicButEmpty_ThenReturnsNone()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeNone();
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasicButOnlyUsername_ThenReturnsOnlyUsername()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("ausername:"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeSome("ausername");
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasicButOnlyUsernameNoDelimiter_ThenReturnsOnlyUsername()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("ausername"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeSome("ausername");
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasicButOnlyPassword_ThenReturnsOnlyPassword()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":apassword"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeNone();
        result.Password.Should().BeSome("apassword");
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasic_ThenReturnsFullCredentials()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("ausername:apassword"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeSome("ausername");
        result.Password.Should().BeSome("apassword");
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndNoQueryParameterAndNoBasicAuthUsername_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndAuthorizationHeaderButNoCredentials_ThenReturnsNone()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndAuthorizationHeaderButOnlyPassword_ThenReturnsNone()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":apassword"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndAuthorizationHeaderWithRandomUsernameOnly_ThenReturnsNone()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("anapikey:"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndAuthorizationHeaderWithApiKeyAsUsernameOnly_ThenReturnsAPIKey()
    {
        var apiKey = new TokensService().CreateAPIKey().ApiKey;
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, $"Basic {credentials}" }
        });
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeSome(apiKey);
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndQueryParameterEmpty_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection
        (new Dictionary<string, StringValues>
        {
            { HttpConstants.QueryParams.APIKey, "" }
        }));

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndQueryParameter_ThenReturnsAPIKey()
    {
        var apiKey = new TokensService().CreateAPIKey().ApiKey;
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection
        (new Dictionary<string, StringValues>
        {
            { HttpConstants.QueryParams.APIKey, apiKey }
        }));

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeSome(apiKey);
    }

    [Fact]
    public void WhenGetHMACAuthAndNoSignature_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());

        var result = httpRequest.Object.GetHMACAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetHMACAuthAndSignatureHasEmptyValue_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.HMACSignature, "" }
        });

        var result = httpRequest.Object.GetHMACAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetHMACAuthAndSignatureHasValue_ThenReturnsSignature()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.HMACSignature, "asignature" }
        });

        var result = httpRequest.Object.GetHMACAuth();

        result.Should().BeSome("asignature");
    }

    [Fact]
    public void WhenIsContentTypeAndNoContentType_ThenReturnsFalse()
    {
        var request = new Mock<HttpRequest>();

        var result = request.Object.IsContentType(string.Empty);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsContentTypeAndDifferentContentType_ThenReturnsFalse()
    {
        var request = new Mock<HttpRequest>();
        request.Setup(req => req.ContentType).Returns(HttpConstants.ContentTypes.Xml);

        var result = request.Object.IsContentType(HttpConstants.ContentTypes.Json);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsContentTypeAndJsonWithoutCharSet_ThenReturnsTrue()
    {
        var request = new Mock<HttpRequest>();
        request.Setup(req => req.ContentType).Returns(HttpConstants.ContentTypes.Json);

        var result = request.Object.IsContentType(HttpConstants.ContentTypes.Json);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsContentTypeAndJsonWithCharSet_ThenReturnsTrue()
    {
        var request = new Mock<HttpRequest>();
        request.Setup(req => req.ContentType).Returns(HttpConstants.ContentTypes.JsonWithCharset);

        var result = request.Object.IsContentType(HttpConstants.ContentTypes.Json);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsContentTypeAndMultiPartFormDataWithBoundary_ThenReturnsTrue()
    {
        var request = new Mock<HttpRequest>();
        request.Setup(req => req.ContentType)
            .Returns($"{HttpConstants.ContentTypes.MultiPartFormData}; boundary=\"aboundary\"");

        var result = request.Object.IsContentType(HttpConstants.ContentTypes.MultiPartFormData);

        result.Should().BeTrue();
    }
}