# Get an identity spike (.Net) - Focus on addressing the risks identified in the initial [Ruby spike](/docs/tech-spike-write-ups/get-an-identity-oidc-oauth-with-ruby.md)


## Summary
The spike to build “Get an identity” as a Single Sign-on (SSO) provider based on OAuth v2.0 and OpenID using .Net and OpenIddict librariesConne was successful. We took the [decision](/docs/architecture/decisions/0005-use-dotNet-C%23-for-get-an-identity-auth-server.md) to:

1. Build "Get an Identity" as an Auth Server.
2. Build "Get an Identity" using .Net C#

In this spike we were able to quickly (and as out of the box as possible - using the libraries) implement:

* Teacher Account / Get an Identity
* Secure Access to TS data claims
* Service to service integration using client credentials
* Ability to hook in the current UI designs to relevant flows


Things we didn't test (but in theory should work)
* Join a teachers data across TS services using the Identity Service


This document outlines our approach to testing if .Net could be used to build the service and focussed on the riskiest findings from the initial spike in Ruby.


## .Net OAuth/OIDC/Resource Server Tech Spike Approach

## 1. Technology:
* How easy is it to get an OIDC & OAuth server up and running using .Net using the open source ​​https://github.com/openiddict/ libraries?

In short, we found that .Net has a mature set of available libraries to implement OAUTH and OIDC flows, [our ADR's give more detail](/docs/architecture/decisions/0005-use-dotNet-C%23-for-get-an-identity-auth-server.md).

## 2. Functionality Focus:

We created a simple [test application](/dotnet-authserver) that was enough to test the basic Auth-Code flow and client credentials flows (which we hypothesised were the main flows we will need to implement for service integrations).

* Logging a user into an account (auth/code or ID Token), using 1. an email + PIN code 2. an email + other fields (as per design history)

* Custom resource scopes, optional consent UI providing an authorisation grant to a data scope e.g. “I grant access for [service x] to get my personal data stored in my records. “ But also allowing this to be silent.

* Back end to back end service integration: Client credentials - use case: to integrate service to service comms e.g. Find a lost TRN calling DQT for matching records via Qualifications API e.g “Find”.

* Test Auth server UI (Record checking - “we don’t know who you are?” -  using DQT (via TRN’s), ensure this can scale e.g match on something else?) 

## Other findings

Backing up earlier HLD's the spike supported the hypothesis that is would be easier for "Get an Identity" to create the matching UI (as opposed to Find a Lost TRN) as it would be less complex and chatty in code terms, less coupling to external components. We also anticipate other services having differeing needs around matching, therefore the we think this would mean a simpler workflow (code, test, deploy journey on both Auth Server and Find).
 