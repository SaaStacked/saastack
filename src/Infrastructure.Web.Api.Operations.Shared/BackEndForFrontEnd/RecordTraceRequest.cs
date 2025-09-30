using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Records a trace event in the product
/// </summary>
[Route("/record/trace", OperationMethod.Post)]
public class RecordTraceRequest : UnTenantedEmptyRequest<RecordTraceRequest>
{
    public Dictionary<string, object?>? Arguments { get; set; }

    [Required] public string? Level { get; set; }

    [Required] public string? MessageTemplate { get; set; }
}