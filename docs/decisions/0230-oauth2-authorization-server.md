# OAuth2 Authorization Server

* status: accepted
* date: 2026-01-01
* deciders: jezzsantos

# Context and Problem Statement

SaaStack implements a basic OAuth2 Authorization Server (with client credentials), and OIDC Authorization Code Flows (see [RFC6749](https://datatracker.ietf.org/doc/html/rfc6749) and [OpenID Connect](https://openid.net/specs/openid-connect-core-1_0.html)).

The backend API has full support for the Authorization Code Flow, and allows Operators (user with the `std_ops` role) to create/configure OAuth2 client applications, and generate client secrets for them, such that those 3rd party integrations can access the API on behalf of their own users. This makes ofr powerful integrations.

However, the backend APIs are public and accessible anonymously, but they are also stateless, and the UI components that they need in specific OAuth2 flows are deployed in the WebsiteHost pages. Such that when the backend APIs are called they may return `HTTP-302` redirects to the WebsiteHost pages, which are then rendered in the browser.

The pages in the browser can maintain state about whether the user is authenticated, but the backend APIs cannot.

The challenge comes when a client application uses the backend APIs directly to complete the OAuth2 flows, and the user is not authenticated. In this case, the backend APIs will return a `HTTP-302` redirect pointing to the WebsiteHost pages, which will then render the login page in the browser. The user will authenticate themselves in the JS App but there will be no continuation back to the authorization API after that point, since the original authorize request that was made (`GET /oauth2/authorize?client_id=...&redirect_uri=...&response_type=code&scope=...&state=...&nonce=...&code_challenge=...&code_challenge_method=...`) will be lost to the browser app.

We either need to expect the client to manage all their own state, or we can recruit the BEFFE (and JS App) to manage the whole flow for us end to end, and thus, move the public endpoints to the JS App, instead of the backend APIs (or the BEFFE APIs).

## (Optional) Decision Drivers

To make the whole experience work in simple use cases for external clients. 

## Considered Options

The options are:
1. JS APP Pages
2. BEFFE API
3. Backend API + Extensive integration work

## Decision Outcome

`JS APP Pages`
- It is the normal user experience for all OAuth2 Authorization servers on the internet.
- The easiest to integrate 3rd party applications
- Requires the most work

## (Optional) More Information

We have documented the flows [here](../images/OpenIdConnect-Authorization-Code-Flow.png)