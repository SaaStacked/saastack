using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;

/// <summary>
///     Makes a generic OAuth2 authorization grant request.
/// </summary>
[Route("/oauth2/authorize", OperationMethod.Get)]
[UsedImplicitly]
public class GenericOAuth2AuthorizeRequest : WebRequestEmpty<GenericOAuth2AuthorizeRequest>
{
    [Required]
    [JsonPropertyName("client_id")] public string? ClientId { get; set; }

    [JsonPropertyName("code_challenge")] public string? CodeChallenge { get; set; }

    [JsonPropertyName("code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }
    
    [JsonPropertyName("nonce")] public string? Nonce { get; set; }

    [Required]
    [JsonPropertyName("redirect_uri")] public string? RedirectUri { get; set; }

    [Required]
    [JsonPropertyName("response_type")] public string? ResponseType { get; set; }

    [JsonPropertyName("scope")] public string? Scope { get; set; }

    [JsonPropertyName("state")] public string? State { get; set; }
}