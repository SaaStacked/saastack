extern alias NonFrameworkAnalyzers;
extern alias CommonAnalyzers;
using CommonAnalyzers::Tools.Analyzers.Common;
using NonFrameworkAnalyzers::Application.Interfaces;
using NonFrameworkAnalyzers::Infrastructure.Web.Api.Interfaces;
using NonFrameworkAnalyzers::JetBrains.Annotations;
using Xunit;
using ApiLayerAnalyzer = NonFrameworkAnalyzers::Tools.Analyzers.NonFramework.ApiLayerAnalyzer;
using TypeExtensions = NonFrameworkAnalyzers::Tools.Analyzers.NonFramework.TypeExtensions;

namespace Tools.Analyzers.NonFramework.UnitTests;

[UsedImplicitly]
public class ApiLayerAnalyzerSpec
{
    [UsedImplicitly]
    public class GivenAWebApiService
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenAnyRule
        {
            [Fact]
            public async Task WhenInExcludedNamespace_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace Common;
public class AClass : IWebApiService
{
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenNotWebApiClass_ThenNoAlert()
            {
                const string input = @"
namespace ANamespace;
public class AClass
{
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasNoMethods_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPrivateMethod_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    private void AMethod(){}
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasInternalMethod_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    internal void AMethod(){}
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule010
        {
            [Fact]
            public async Task WhenHasPublicMethodWithVoidReturnType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    public void AMethod(){}
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule010,
                    input, 6, 17, "AMethod",
                    TypeExtensions.Stringify(ApiLayerAnalyzer.AllowableServiceOperationReturnTypes));
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskReturnType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task AMethod(){ return Task.CompletedTask; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule010,
                    input, 7, 17, "AMethod",
                    TypeExtensions.Stringify(ApiLayerAnalyzer.AllowableServiceOperationReturnTypes));
            }

            [Fact]
            public async Task WhenHasPublicMethodWithWrongTaskReturnType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<string> AMethod(){ return Task.FromResult(string.Empty); }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule010,
                    input, 7, 25, "AMethod",
                    TypeExtensions.Stringify(ApiLayerAnalyzer.AllowableServiceOperationReturnTypes));
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiEmptyResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public Task<ApiEmptyResult> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return Task.FromResult<ApiEmptyResult>(() => new Result<EmptyResponse, Error>());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public Task<ApiResult<TestResource, TestResponse>> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{
        return Task.FromResult<ApiResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiPostResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public Task<ApiPostResult<TestResource, TestResponse>> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{
        return Task.FromResult<ApiPostResult<TestResource, TestResponse>>(() => new Result<PostResult<TestResponse>, Error>());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiGetResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public Task<ApiGetResult<TestResource, TestResponse>> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{
        return Task.FromResult<ApiGetResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiSearchResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public Task<ApiSearchResult<TestResource, TestSearchResponse>> AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{
        return Task.FromResult<ApiSearchResult<TestResource, TestSearchResponse>>(() => new Result<TestSearchResponse, Error>());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiPutPatchResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public Task<ApiPutPatchResult<TestResource, TestResponse>> AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{
        return Task.FromResult<ApiPutPatchResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiDeleteResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public Task<ApiDeleteResult> AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{
        return Task.FromResult<ApiDeleteResult>(() => new Result<EmptyResponse, Error>());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithWrongNakedReturnType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    public string AMethod(){ return string.Empty; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule010,
                    input, 6,
                    19, "AMethod",
                    TypeExtensions.Stringify(ApiLayerAnalyzer
                        .AllowableServiceOperationReturnTypes));
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiEmptyResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiResult<TestResource, TestResponse> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{
        return () => new Result<TestResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiPostResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPostResult<TestResource, TestResponse> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{
        return () => new Result<PostResult<TestResponse>, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
            
            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiRedirectResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiRedirectResult<TestResource, TestResponse> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{
        return () => new Result<RedirectResult<TestResponse>, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiGetResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiGetResult<TestResource, TestResponse> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{
        return () => new Result<TestResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiSearchResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiSearchResult<TestResource, TestSearchResponse> AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{
        return () => new Result<TestSearchResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiPutPatchResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPutPatchResult<TestResource, TestResponse> AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{
        return () => new Result<TestResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiDeleteResultReturnType_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiDeleteResult AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule011And012
        {
            [Fact]
            public async Task WhenHasNoParameters_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod()
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule011,
                    input, 8, 27, "AMethod");
            }

            [Fact]
            public async Task WhenHasTooManyParameters_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request, CancellationToken cancellationToken, string value)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule011,
                    input, 10, 27, "AMethod");
            }

            [Fact]
            public async Task WhenFirstParameterIsNotRequestType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(string value)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule011,
                    input, 8, 27, "AMethod");
            }

            [Fact]
            public async Task WhenSecondParameterIsNotCancellationToken_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request, string value)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule012,
                    input, 9, 27, "AMethod");
            }

            [Fact]
            public async Task WhenOnlyRequest_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenRequestAndCancellation_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request, CancellationToken cancellationToken)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule013AndRule017
        {
            [Fact]
            public async Task WhenHasNoAttributes_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestNoRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(input,
                    (ApiLayerAnalyzer.Rule013, 9, 27, "AMethod", null),
                    (ApiLayerAnalyzer.Rule017, 9, 35, nameof(TestNoRouteAttributeRequest), null));
            }

            [Fact]
            public async Task WhenMissingAttribute_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    [TestAttribute]
    public ApiEmptyResult AMethod({nameof(TestNoRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(input,
                    (ApiLayerAnalyzer.Rule013, 10, 27, "AMethod", null),
                    (ApiLayerAnalyzer.Rule017, 10, 35, nameof(TestNoRouteAttributeRequest), null));
            }

            [Fact]
            public async Task WhenAttribute_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule014
        {
            [Fact]
            public async Task WhenOneRoute_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod1({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenTwoWithSameRoute_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod1({nameof(TestGetRouteAttributeRequest1)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod2({nameof(TestGetRouteAttributeRequest2)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenThreeWithSameRouteFirstSegment_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod1({nameof(TestGetRouteAttributeRequest1)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod2({nameof(TestGetRouteAttributeRequest2)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod3({nameof(TestGetRouteAttributeRequest3)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDifferentRouteSegments_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod1({nameof(TestGetRouteAttributeRequest1)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod2({nameof(TestGetRouteAttributeRequest2)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod4({nameof(TestGetRouteAttributeRequest4)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule014,
                    input, 17, 27, "AMethod4");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule015
        {
            [Fact]
            public async Task WhenNoDuplicateRequests_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDuplicateRequests_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod1({nameof(TestGetRouteAttributeRequest1)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod2({nameof(TestGetRouteAttributeRequest1)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod3({nameof(TestGetRouteAttributeRequest2)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(input,
                    (ApiLayerAnalyzer.Rule015, 9, 27, "AMethod1", null),
                    (ApiLayerAnalyzer.Rule020, 9, 27, "AMethod1", null),
                    (ApiLayerAnalyzer.Rule015, 13, 27, "AMethod2", null),
                    (ApiLayerAnalyzer.Rule020, 13, 27, "AMethod2", null));
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule016
        {
            [Fact]
            public async Task WhenPostAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPostAndReturnsApiResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiResult<string, TestResponse> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 44, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiResult<string, TestResponse> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiResult<string, TestResponse> AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiResult<string, TestResponse> AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiResult<string, TestResponse> AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPostAndReturnsApiPostResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPostResult<TestResource, TestResponse> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{ 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAndReturnsApiPostResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPostResult<TestResource, TestResponse> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 54, "AMethod", OperationMethod.Get, ExpectedAllowedResultTypes(OperationMethod.Get));
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiPostResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPostResult<TestResource, TestResponse> AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{ 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 54, "AMethod", OperationMethod.Search,
                    ExpectedAllowedResultTypes(OperationMethod.Search));
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiPostResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPostResult<TestResource, TestResponse> AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{ 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 54, "AMethod", OperationMethod.PutPatch,
                    ExpectedAllowedResultTypes(OperationMethod.PutPatch));
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiPostResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPostResult<TestResource, TestResponse> AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{ 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 54, "AMethod", OperationMethod.Delete,
                    ExpectedAllowedResultTypes(OperationMethod.Delete));
            }

            [Fact]
            public async Task WhenPostAndReturnsApiGetResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiGetResult<string, TestResponse> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 47, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiGetResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiGetResult<string, TestResponse> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiGetResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiGetResult<string, TestResponse> AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiGetResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiGetResult<string, TestResponse> AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 47, "AMethod", OperationMethod.PutPatch,
                    ExpectedAllowedResultTypes(OperationMethod.PutPatch));
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiGetResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiGetResult<string, TestResponse> AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 47, "AMethod", OperationMethod.Delete,
                    ExpectedAllowedResultTypes(OperationMethod.Delete));
            }

            [Fact]
            public async Task WhenPostAndReturnsApiSearchResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiSearchResult<string, TestSearchResponse> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 56, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiSearchResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiSearchResult<string, TestSearchResponse> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 56, "AMethod", OperationMethod.Get, ExpectedAllowedResultTypes(OperationMethod.Get));
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiSearchResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiSearchResult<string, TestSearchResponse> AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiSearchResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiSearchResult<string, TestSearchResponse> AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 56, "AMethod", OperationMethod.PutPatch,
                    ExpectedAllowedResultTypes(OperationMethod.PutPatch));
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiSearchResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiSearchResult<string, TestSearchResponse> AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 56, "AMethod", OperationMethod.Delete,
                    ExpectedAllowedResultTypes(OperationMethod.Delete));
            }

            [Fact]
            public async Task WhenPostAndReturnsApiPutPatchResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPutPatchResult<string, TestResponse> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 52, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiPutPatchResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPutPatchResult<string, TestResponse> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 52, "AMethod", OperationMethod.Get, ExpectedAllowedResultTypes(OperationMethod.Get));
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiPutPatchResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPutPatchResult<string, TestResponse> AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 52, "AMethod", OperationMethod.Search,
                    ExpectedAllowedResultTypes(OperationMethod.Search));
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiPutPatchResult_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPutPatchResult<string, TestResponse> AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiPutPatchResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiPutPatchResult<string, TestResponse> AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{ 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 52, "AMethod", OperationMethod.Delete,
                    ExpectedAllowedResultTypes(OperationMethod.Delete));
            }

            [Fact]
            public async Task WhenPostAndReturnsApiDeleteResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiDeleteResult AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 28, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiDeleteResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiDeleteResult AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 28, "AMethod", OperationMethod.Get, ExpectedAllowedResultTypes(OperationMethod.Get));
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiDeleteResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiDeleteResult AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 28, "AMethod", OperationMethod.Search,
                    ExpectedAllowedResultTypes(OperationMethod.Search));
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiDeleteResult_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiDeleteResult AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 28, "AMethod", OperationMethod.PutPatch,
                    ExpectedAllowedResultTypes(OperationMethod.PutPatch));
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiDeleteResult_ThenNotAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiDeleteResult AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPostAndReturnsApiRedirectResult_ThenNotAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiRedirectResult<TestResource, TestResponse> AMethod({nameof(TestPostRouteAttributeRequest)} request)
    {{ 
        return () => new RedirectResult<TestResponse>(new TestResponse(), ""/aurl"");
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAndReturnsApiRedirectResult_ThenNotAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiRedirectResult<TestResource, TestResponse> AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new RedirectResult<TestResponse>(new TestResponse(), ""/aurl"");
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiRedirectResult_ThenNotAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiRedirectResult<TestResource, TestResponse> AMethod({nameof(TestSearchRouteAttributeRequest)} request)
    {{ 
        return () => new RedirectResult<TestResponse>(new TestResponse(), ""/aurl"");
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiRedirectResult_ThenNotAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiRedirectResult<TestResource, TestResponse> AMethod({nameof(TestPutPatchRouteAttributeRequest)} request)
    {{ 
        return () => new RedirectResult<TestResponse>(new TestResponse(), ""/aurl"");
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiRedirectResult_ThenNotAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiRedirectResult<TestResource, TestResponse> AMethod({nameof(TestDeleteRouteAttributeRequest)} request)
    {{ 
        return () => new RedirectResult<TestResponse>(new TestResponse(), ""/aurl"");
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            private static string ExpectedAllowedResultTypes(OperationMethod method)
            {
                return TypeExtensions.Stringify(ApiLayerAnalyzer
                    .AllowableOperationReturnTypes[method].ToArray());
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule018AndRule019
        {
            [Fact]
            public async Task WhenRouteIsAnonymousAndMissingAuthorizeAttribute_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestAnonymousRouteNoAuthorizeAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenRouteIsNotAnonymousAndAuthorizeAttribute_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestSecureRouteAuthorizeAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenRouteIsAnonymousAndAuthorizeAttribute_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestAnonymousRouteAuthorizeAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";
                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule018,
                    input, 9, 35, nameof(TestAnonymousRouteAuthorizeAttributeRequest));
            }

            [Fact]
            public async Task WhenRouteIsNotAnonymousAndNoAuthorizeAttribute_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestSecureRouteNoAuthorizeAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";
                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule019,
                    input, 9, 35, nameof(TestSecureRouteNoAuthorizeAttributeRequest));
            }

            [Fact]
            public async Task WhenRouteIsPrivateInterHostAndMissingAuthorizeAttribute_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestPrivateInterHostRouteNoAuthorizeAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenRouteIsPrivateInterHostAndAuthorizeAttribute_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestPrivateInterHostRouteAuthorizeAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";
                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule020
        {
            [Fact]
            public async Task WhenNoDuplicateRequests_ThenNoAlert()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod({nameof(TestGetRouteAttributeRequest)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDuplicateRequests_ThenAlerts()
            {
                const string input = $@"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonFramework.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{{
    public ApiEmptyResult AMethod1({nameof(TestGetRouteAttributeRequest1)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod2({nameof(TestGetRouteAttributeRequest5)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
    public ApiEmptyResult AMethod3({nameof(TestGetRouteAttributeRequest2)} request)
    {{ 
        return () => new Result<EmptyResponse, Error>();
    }}
}}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule020,
                    input,
                    (9, 27, "AMethod1"),
                    (13, 27, "AMethod2"));
            }
        }
    }

    [UsedImplicitly]
    public class GivenARequest
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenRule030
        {
            [Fact]
            public async Task WhenIsNotPublic_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
internal class ARequest : IWebRequest
{
    public string? AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule030, input, 9, 16, "ARequest");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule031
        {
            [Fact]
            public async Task WhenIsNotNamedCorrectly_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class AClass : IWebRequest
{
    public string? AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule031, input, 9, 14, "AClass");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule032
        {
            [Fact]
            public async Task WhenIsNotInCorrectAssembly_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace anamespace;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public string? AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule032, input, 9, 14, "ARequest",
                    AnalyzerConstants.ServiceOperationTypesNamespace);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule033
        {
            [Fact]
            public async Task WhenHasNoRouteAttribute_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
public class ARequest : IWebRequest
{
    public string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule033, input, 8, 14, "ARequest");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule034
        {
            [Fact]
            public async Task WhenHasCtorAndNotParameterless_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public ARequest(string value)
    {
        AProperty = value;
    }

    public string? AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule034, input, 9, 14, "ARequest");
            }

            [Fact]
            public async Task WhenHasCtorAndPrivate_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    private ARequest()
    {
        AProperty = string.Empty;
    }

    public string? AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule034, input, 9, 14, "ARequest");
            }

            [Fact]
            public async Task WhenHasCtorAndIsParameterless_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public ARequest()
    {
        AProperty = string.Empty;
    }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule035
        {
            [Fact]
            public async Task WhenAnyPropertyHasNoSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public string? AProperty { get; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule035, input, 11, 20, "AProperty");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule036
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsOptional_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule036, input, 12, 29, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule037
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsRequired_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule037, input, 12, 28, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsRequired_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public required int AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule037, input, 12, 25, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyEnumIsRequired_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public required AnEnum AProperty { get; set; }
}

public enum AnEnum
{
    AValue
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule037, input, 12, 28, "AProperty");
            }
        }

        [UsedImplicitly]
        public class GivenRule038
        {
            [Trait("Category", "Unit.Tooling")]
            public class GivenAGetRequest
            {
                [Fact]
                public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public string? AProperty { get; set; }
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }

                [Fact]
                public async Task WhenAnyPropertyReferenceTypeIsNotNullable_ThenAlerts()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public string AProperty { get; set; }
}";

                    await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                        ApiLayerAnalyzer.Rule038, input, 12, 19, "AProperty");
                }

                [Fact]
                public async Task WhenAnyPropertyValueTypeIsNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public int? AProperty { get; set; }
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }

                [Fact]
                public async Task WhenAnyPropertyValueTypeIsNotNullable_ThenAlerts()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public int AProperty { get; set; }
}";

                    await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                        ApiLayerAnalyzer.Rule038, input, 12, 16, "AProperty");
                }

                [Fact]
                public async Task WhenAnyPropertyEnumIsNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public AnEnum? AProperty { get; set; }
}

public enum AnEnum
{
    AValue
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }

                [Fact]
                public async Task WhenAnyPropertyEnumIsNotNullable_ThenAlerts()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public AnEnum AProperty { get; set; }
}

public enum AnEnum
{
    AValue
}";

                    await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                        ApiLayerAnalyzer.Rule038, input, 12, 19, "AProperty");
                }
            }

            [Trait("Category", "Unit.Tooling")]
            public class GivenAPostRequest
            {
                [Fact]
                public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public string? AProperty { get; set; }
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }

                [Fact]
                public async Task WhenAnyPropertyReferenceTypeIsNotNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public string AProperty { get; set; }
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }

                [Fact]
                public async Task WhenAnyPropertyValueTypeIsNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public int? AProperty { get; set; }
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }

                [Fact]
                public async Task WhenAnyPropertyValueTypeIsNotNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public int AProperty { get; set; }
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }

                [Fact]
                public async Task WhenAnyPropertyEnumIsNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public AnEnum? AProperty { get; set; }
}

public enum AnEnum
{
    AValue
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }

                [Fact]
                public async Task WhenAnyPropertyEnumIsNotNullable_ThenNoAlert()
                {
                    const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
/// asummary
/// </summary>
[Route(""/apath"", OperationMethod.Post)]
public class ARequest : IWebRequest
{
    public AnEnum AProperty { get; set; }
}

public enum AnEnum
{
    AValue
}";

                    await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
                }
            }
        }
    }

    [UsedImplicitly]
    public class GivenAResponse
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenRule040
        {
            [Fact]
            public async Task WhenIsNotPublic_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
internal class AResponse : IWebResponse
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule040, input, 5, 16, "AResponse");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule041
        {
            [Fact]
            public async Task WhenIsNotNamedCorrectly_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AClass : IWebResponse
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule041, input, 5, 14, "AClass");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule042
        {
            [Fact]
            public async Task WhenIsNotInCorrectAssembly_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace anamespace;
public class AResponse : IWebResponse
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule042, input, 5, 14, "AResponse",
                    AnalyzerConstants.ServiceOperationTypesNamespace);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        [Trait("Category", "Unit.Tooling")]
        public class GivenRule043
        {
            [Fact]
            public async Task WhenHasCtorAndNotParameterless_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public AResponse(string value)
    {
        AProperty = value;
    }

    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule043, input, 5, 14, "AResponse");
            }

            [Fact]
            public async Task WhenHasCtorAndPrivate_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    private AResponse()
    {
        AProperty = string.Empty;
    }

    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule043, input, 5, 14, "AResponse");
            }

            [Fact]
            public async Task WhenHasCtorAndIsParameterless_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public AResponse()
    {
        AProperty = string.Empty;
    }

    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule044
        {
            [Fact]
            public async Task WhenAnyPropertyHasNoSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public string? AProperty1 { get; }

    public required string AProperty2 { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule044, input, 7, 20, "AProperty1");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule045
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsOptional_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule045, input, 8, 29, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }
    }
}

[UsedImplicitly]
public class TestResource;

[UsedImplicitly]
public class TestResponse : IWebResponse;

[UsedImplicitly]
public class TestSearchResponse : IWebSearchResponse
{
    public SearchResultMetadata Metadata { get; set; } = new();
}

[UsedImplicitly]
public class TestNoRouteAttributeRequest : WebRequest<TestNoRouteAttributeRequest, TestResponse>;

[Route("/aresource", OperationMethod.Search)]
[UsedImplicitly]
public class TestSearchRouteAttributeRequest : WebRequest<TestSearchRouteAttributeRequest, TestResponse>;

[Route("/aresource", OperationMethod.Post)]
[UsedImplicitly]
public class TestPostRouteAttributeRequest : WebRequest<TestPostRouteAttributeRequest, TestResponse>;

[Route("/aresource", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest : WebRequest<TestGetRouteAttributeRequest, TestResponse>;

[Route("/aresource/1", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest1 : WebRequest<TestGetRouteAttributeRequest1, TestResponse>;

[Route("/aresource/2", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest2 : WebRequest<TestGetRouteAttributeRequest2, TestResponse>;

[Route("/aresource/3", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest3 : WebRequest<TestGetRouteAttributeRequest3, TestResponse>;

[Route("/anotherresource/1", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest4 : WebRequest<TestGetRouteAttributeRequest4, TestResponse>;

[Route("/aresource/1", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest5 : WebRequest<TestGetRouteAttributeRequest5, TestResponse>;

[Route("/aresource", OperationMethod.PutPatch)]
[UsedImplicitly]
public class TestPutPatchRouteAttributeRequest : WebRequest<TestPutPatchRouteAttributeRequest, TestResponse>;

[Route("/aresource", OperationMethod.Delete)]
[UsedImplicitly]
public class TestDeleteRouteAttributeRequest : WebRequest<TestDeleteRouteAttributeRequest, TestResponse>;

[AttributeUsage(AttributeTargets.Method)]
[UsedImplicitly]
public class TestAttribute : Attribute;

[Route("/aresource", OperationMethod.Post)]
[UsedImplicitly]
public class
    TestAnonymousRouteNoAuthorizeAttributeRequest : WebRequest<TestAnonymousRouteNoAuthorizeAttributeRequest,
    TestResponse>;

[Route("/aresource", OperationMethod.Post)]
[Authorize(Roles.Platform_Standard)]
[UsedImplicitly]
public class
    TestAnonymousRouteAuthorizeAttributeRequest : WebRequest<TestAnonymousRouteAuthorizeAttributeRequest, TestResponse>;

[Route("/aresource", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard)]
[UsedImplicitly]
public class
    TestSecureRouteAuthorizeAttributeRequest : WebRequest<TestSecureRouteAuthorizeAttributeRequest, TestResponse>;

[Route("/aresource", OperationMethod.Post, AccessType.Token)]
[UsedImplicitly]
public class
    TestSecureRouteNoAuthorizeAttributeRequest : WebRequest<TestSecureRouteNoAuthorizeAttributeRequest, TestResponse>;
    
[Route("/aresource", OperationMethod.Post, AccessType.PrivateInterHost)]
[UsedImplicitly]
public class
    TestPrivateInterHostRouteNoAuthorizeAttributeRequest : WebRequest<TestPrivateInterHostRouteNoAuthorizeAttributeRequest,
    TestResponse>;

[Route("/aresource", OperationMethod.Post, AccessType.PrivateInterHost)]
[Authorize(Roles.Platform_Standard)]
[UsedImplicitly]
public class
    TestPrivateInterHostRouteAuthorizeAttributeRequest : WebRequest<TestPrivateInterHostRouteAuthorizeAttributeRequest,
    TestResponse>;
