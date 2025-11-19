using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;

/// <summary>
///     Makes a generic OAuth2 authorization grant request.
/// </summary>
[Route("/auth/token", OperationMethod.Get)]
[UsedImplicitly]
public class GenericOAuth2GrantAuthorizationRequest : WebRequest<GenericOAuth2GrantAuthorizationRequest,
    GenericOAuth2GrantAuthorizationResponse>
{
    [JsonPropertyName("client_id")] public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")] public string? ClientSecret { get; set; }

    [JsonPropertyName("code")] public string? Code { get; set; }

    [JsonPropertyName("code_challenge")] public string? CodeChallenge { get; set; }

    [JsonPropertyName("code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    [JsonPropertyName("code_verifier")] public string? CodeVerifier { get; set; }

    [JsonPropertyName("grant_type")] public string? GrantType { get; set; }

    [JsonPropertyName("nonce")] public string? Nonce { get; set; }

    [JsonPropertyName("redirect_uri")] public string? RedirectUri { get; set; }

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }

    [JsonPropertyName("response_type")] public string? ResponseType { get; set; }

    [JsonPropertyName("scope")] public string? Scope { get; set; }

    [JsonPropertyName("state")] public string? State { get; set; }
}