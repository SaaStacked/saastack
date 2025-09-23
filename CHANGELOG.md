# How It Works

> This change log is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

> All releases are documented in this file, in reverse order.

> We keep at least one `[Unreleased]` section that is to be used to capture changes as you work. When the release is ready, this section is versioned and moved down the file (past the horizontal break), AND a new `[Unreleased]` is created for the next release.

---

## [Unreleased]

### Non-breaking Changes

### Breaking Changes

### Fixed

---

## [1.0.0] - 2025-09-21

### Non-breaking Changes
- We have changed the expiry of the `auth-tok` cookie (produced by the BEFFE) to be the same time period as the `authref-tok` cookie. This is to help create a "refreshable session", for the browser application, that has it submit an expired token to the backend API to reject as unauthorized. This is needed otherwise the cookie just expires and disappears, forcing the user to login again, every 15 minutes. With this scheme, the browser can now respond to a `HTTP - 401` by refreshing the token, and retrying the request.
- Changed authentication cookies `auth-tok` and `authref-tok` to store the expiry date as well as the JWT token in a JSON structure.
- Chose React as our JS App framework served from the `WebsiteHost`BEFFE project.
- Added support for: Vite (build and test), Tailwind CSS, i18next localization, offline support, and implemented the JavaScript Action with `@tanstack/react-query`.
- Added a basic set of UI pages and re-usable components for many of the most common UI scenarios.
- Added a StoryBook for all components, and some pages.
- We have added a missing API for sending a registration confirmation emails. In cases where the first email expired, or went missing.
- The authorization policy for roles and features has been renamed from `POLICY:` to `RolesAndFeatures:`, and other policies have been slightly modified.
- Updated some of the BEFFE APIs that call through to the backend APIs, so that they exclude any `Authorization` header (if appended to the request by the BEFFE). This is to prevent getting an accidental `HTTP - 401` for an expired token, when calling certain APIs like `RefreshToken` or `Authenticate` or `GetAllFeatureFlags` which we cannot risk being called with an expired token (inside the AuthN cookies). These API calls cannot risk being rejected by the backend API, for an expired token. All other calls through the reverse Proxy will automatically include the `Authorization` header that includes the JWT token, if the request is authenticated at that time. This is very intentional to trigger the JS App to refresh the expired token, as designed.
- Changed the SaaStack branding.

### Breaking Changes
- (Potentially breaking) By default in ASPNET, when an endpoint is not marked with a `RequireAuthorization("apolicy")` ASPNET does not validate any authorization proof in the request (i.e. HMAC, APiKeys, Auth Cookies, etc.) This is an unexpected problem, that we haven't encountered before getting the JS App working properly.
  - Added a `RequireAuthorization("Anonymous")` policy to all anonymous endpoints, and implemented to policy to authenticate any proof passed in the request, if present.
  - Invalid proofs (e.g. expired JWT tokens) will now be validated and rejected on all Anonymous endpoints.
  - This will only affect clients that send invalid proofs (like an expired JWT token) to an anonymous endpoints.
  - This is exactly the scenario we want to occur for the JS App to implement the intended auto-refresh mechanism.

### Fixed
- Locale and Timezone were persisted in the `EndUserProfile` value object, but this data was incorrectly mapped to the `Registered` event in the `EndUsersRoot`,and therefore the value read by the `UserProfile` application defaulted to `en-US` and `UTC`.
- When handling BasicAuth, or ApiKeyAuth, the extraction of username and password from the `Authorization` header was not correctly handling the case where the colon delimiter was not provided.
- When handling ApiKeyAuth, the extraction of the API Key from the `Authorization` header was not correctly handling the case where the the apikey was not in a valid format of an APIKey.
- ApiKeys are now generated without the colon character.
- BEFFE recorder APIs now handle empty property array correctly
- FlagsmithHttpServiceClient now handles failed requests for flags (remotely) properly now.

---

## [1.0.0] - 2025-08-22

### Non-breaking Changes
- Added new methods for mapping Optionals to Nullables and vice versa for ValuesObjects, Domain Events and Application Layer conversions.
- Native support for `Optional<T>` properties in ValueObjects, wrt to `GetAtomicValues()` and `Rehydrate()` methods.
- `ValueObjectBase<T>.RehydrateToList()` now returns `List<Optional<string>>` instead of `List<string?>` in order to simplify mapping values.
  - Please update all the ValueObject `Rehydrate()` methods to use the new `Optional<string>` return type, utilizing the new syntax, and new extension methods
  - Please update all the ValueObject `GetAtomicValues()` methods to use the new collection syntax, and no longer destruct the `Optional<T>` values, nor `DateTime` values anymore
  
### Breaking Changes
- none

### Fixed
- Roslyn rules now support C#12 syntax for new collections syntax in `ValueObjectBase<T>.GetAtomicValues()`
- Fixed all ValueObjects to use Optionals instead of Nullables, and turned back on Roslyn rules that enforce that.
- Simplified/eliminated the use of nested `Optional<T>` in `CommandEntity` and `QueryEntity` persistence classes.

---

## [1.0.0] - 2025-08-09

### Non-breaking Changes
- Added `Locale` to `UserProfile` resource and all associated APIs, as backwards compatible
- Added `Locale` to `EndUserProfile` value object, but backwards compatible
- Existing domain events that added `Locale`, but are backwards compatible with previous events:
  - `Domain.Events.Shared.EndUsers.Registered`
  - `IdentityDomain.Events.SSOUsers.DetailsChanged`
- Added new domain event `Domain.Events.Shared.UserProfiles.LocaleChanged`

### Breaking Changes
- Read models tables have been added to the main SqlServer database, and will need to be updated:
- (eventing-generic):
  - `SSOUser` table added column `Locale`
  - `UserProfile` table added column `Locale`

---

## [1.0.0] - 2025-08-07

### Non-breaking Changes
- Open ID Connect is now minimally supported, with a new OAuth/OIDC API. Only the 'Authorization Code Flow' is supported, none of the other OAuth2 flows are implemented.
- Two additional APIs groups have been added to support OAuth2 clients and to manage client_secrets and user consents.
- All services of the Identity subdomain, have been abstracted behind the `IIdentityServerProvider` interface. This permits the native implementation to be replaced with a 3rd party implementation, such as Auth0, Okta, IdentityServer, or Keycloak at a later date.
- Location of the `JwtTokenService` has moved from `Infrastructure.Web.Hosting.Common` to `Infrastructure.Web.Hosting.Identity`.
- Added `codeVerifier` to the SSO authentication flow for passing to the 3rd party SSO Provider, in case the case where PKCE was used to obtain the authorization code in the client.

### Breaking Changes
- All security JWT tokens (specifically the `access_token` and `refresh_token` and now `id_token`) that are created by this API, and verified by this API, are now signed using an RSA256 asymmetric algorithm, instead of the previous HMAC-SHA512 symmetric algorithm.
  - Two new configuration settings are required to be added to your deployment, they are: `Hosts:IdentityApi:JWT:PublicKey` and `Hosts:IdentityApi:JWT:PrivateKey`. You can delete the old `Hosts:IdentityApi:JWT:SigningKey` setting, it is no longer used.
  - Any previously issued access_token and refresh_token by the API that may have been stored in any repositories or event stores (Such as the tokens stored in `AuthToken` table) will now not be able to be signature verified. Older ones can be deleted from this table, with no impact.
- New readmodel tables have been added to the main SqlServer database, and will need to be created:
  - (snapshotting-generic) `OpenIdConnectAuthorization`
  - (eventing-generic) `OAuth2Client` and `OAuth2ClientConsent`
  - (snapshotting-generic) The following columns were added to the `AuthToken` table in the SqlServer database:
    - `IdToken`
    - `IdTokenExiresOn`
    - `RefreshTokenDigest`
    - The `RefreshToken` colum data is now encrypted.
    - Existing rows can be safely deleted, and recreated automatically, fully populated when users next login. 
- The domain events of the AuthTokenRoot (`TokensChanged`, and `TokenRefreshed`) will now have encrypted values of all tokens. Whereas before they were the unencrypted raw values. If there are any consumers of these events they will need to be updated to decrypt the values before using them.
- These events both have new (optional) properties for the `IdToken` and `IdTokenExpiresOn` and `RefreshTokenDigest`, but are not event-sourced, by default.

### Fixed
- All APIS now have available a new ApiResult type called `ApiRedirectResult`, which is expected to be used rarely, but when used can be used to return a `HTTP 302-Found` redirect response. This was necessary to implement the new OpenIdConnect endpoints.
- `JsonClient` now recognizes properties marked with the `[FromQuery(Name="aname")]` attribute as well as `[JsonPropertyName("aname")]`, that are used to send requests with query string parameters for GET requests. This was necessary to implement the new OpenIdConnect endpoints.
- Removed the need for using any ASPNET binding attributes, such as `[FromQuery]` on many of the request types, except in exceptional cases. Improved the custom `BindAsync()` as a result, and removed the `[AsParameters]` usage from minimal API generator, and fixed up the affected OpenApi generation.
  - PLease remove the use of any `[FromQuery]` attributes on regular properties of POST, GET, PUTPATCH or DELETE request types. They are only needed in cases where you explicitly want a specific property to be in teh querystring, and not in the body of a POST or PUTPATCH request - which should be rare.

---

## [1.0.0] - YYYY-MM-DD

### Changed

- The codebase was copied on this day
