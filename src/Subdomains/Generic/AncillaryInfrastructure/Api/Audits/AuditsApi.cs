using AncillaryApplication;
using Application.Resources.Shared;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Audits;

public sealed class AuditsApi : IWebApiService
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly ICallerContextFactory _callerFactory;

    public AuditsApi(ICallerContextFactory callerFactory, IAncillaryApplication ancillaryApplication)
    {
        _callerFactory = callerFactory;
        _ancillaryApplication = ancillaryApplication;
    }

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> Deliver(DeliverAuditRequest request,
        CancellationToken cancellationToken)
    {
        var delivered =
            await _ancillaryApplication.DeliverAuditAsync(_callerFactory.Create(), request.Message!, cancellationToken);

        return () => delivered.HandleApplicationResult<bool, DeliverMessageResponse>(_ =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsSent = true }));
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> DrainAll(DrainAllAuditsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ancillaryApplication.DrainAllAuditsAsync(_callerFactory.Create(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

    public async Task<ApiSearchResult<Audit, SearchAllAuditsResponse>> SearchAll(
        SearchAllAuditsRequest request, CancellationToken cancellationToken)
    {
        var audits = await _ancillaryApplication.SearchAllAuditsAsync(_callerFactory.Create(),
            request.SinceUtc, request.OrganizationId!, request.ToSearchOptions(), request.ToGetOptions(),
            cancellationToken);

        return () => audits.HandleApplicationResult(a => new SearchAllAuditsResponse
            { Audits = a.Results, Metadata = a.Metadata });
    }
}