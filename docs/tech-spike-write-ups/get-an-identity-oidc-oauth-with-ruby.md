# Get an identity spike (Ruby) - Risks identified
Author: Theodor Vararu
Date: 2022/07/18
## Summary
While the spike to build “Get an identity” as a Single Sign-on (SSO) provider based on OAuth v2.0 and OpenID Connect was successful, it uncovered a number of significant risks to taking this approach.

This document outlines these risks, and presents some alternative approaches.
## Background
As part of making it easier for teachers to sign into DFE services, and separating their identity from metadata like Teacher Reference Numbers (TRNs), we hypothesised a system that would allow users to authenticate using SSO.

The first system that would benefit from this integration would be the Register for a National Professional Qualification (NPQ) service. More background in this design history entry:

https://tra-digital-design-history.herokuapp.com/get-an-identity/service-design-get-an-identity-npq-registration/

This design is called “TRNless,” as the first point where it would deliver value would be to abstract away a sequence of pages (email, date of birth, full name, NINO, ITT provider). This information is what is necessary to find a user’s TRN in case they do not already know it.

Find a lost TRN is an existing service that provides functionality to find a user’s TRN:

https://find-a-lost-trn.education.gov.uk/start

We hypothesised that the path of least resistance would be to build an identity system inside the Find a lost TRN service, in order to leverage the existing code, and reduce the duplication of effort across services.
Goals
Reduce duplication of effort
Provide a more joined up experience
Remove the requirement for a TRN (fully TRNless) for users that don’t have one
Integrate with GOV.UK Accounts down the line
Spike outcome
A spike was done to integrate a Single Sign-on system with Find a lost TRN and Register for an NPQ:

https://github.com/DFE-Digital/find-a-lost-trn/pull/265
https://github.com/DFE-Digital/npq-registration/pull/465

The spike was successful in taking off-the-shelf ruby gems and creating:

A spec-compliant, OAuth v2.0 and OpenID Connect compatible SSO provider
A client integration in the NPQ registration, also using off the shelf components
Custom claims to expose a user’s TRN

Two more features were thought of for the spike: customising the OAuth consent screens, and using an upstream SSO provider like Auth0 (and later GOV.UK Accounts). These were not attempted in the spike in the interest of saving time, but they should be possible.

While the spike was successful, a number of risks were identified with taking this approach.
Risks identified
Risk #1: Understanding OAuth v2.0 and OpenID Connect
OAuth and OIDC are complex specifications. They were designed to meet the needs of large-scale identity providers, like Okta, Twitter, Facebook, Microsoft. As such, a lot of thought and design has been put into them, but they are chiefly used by the “tech giants” which have significant resources to throw at this problem.

The specifications are flexible enough to meet our needs, but come with a significant learning curve for the team implementing the provider-side of the specification. That team most likely needs at least one OIDC expert to unblock and upskill the rest of the team.

This risk is exacerbated if the team is likely to experience churn or attrition, and doubly so if the SSO integration isn’t “completely standard.” That is the case for the NPQ Registration TRNless work, which further increases the risk.
Risk #2: Maintaining doorkeeper, openid_connect, and other ruby libraries
To produce the spike, the following open source gems were used:

(server) doorkeeper - an OAuth v2.0 provider
(server) doorkeeper-openid_connect - an OpenID Connect extension for doorkeeper
(client) omniauth - an authentication client
(client) omniauth_openid_connect - an OAuth v2.0 and OpenID Connect extension for omniauth

These gems depend on a number of things, but worth mentioning are the following:

openid_connect - the only “certified” OpenID Connect implementation for Ruby
swd - a simple web discovery client library, used by openid_connect

While it was possible to eventually achieve the goals of the spike, a number of issues were identified while working with the gems:

The documentation is poor
Example: doorkeeper’s explanation page for what an “Access Grant” is is a blank document
The libraries have hardcoded defaults that don’t allow for certain use cases
Example: swd hardcodes HTTPS as the only protocol that it will allow for the discovery endpoint, which means it’s not trivial to develop this system locally, without resorting to monkey patching or forking
The libraries are not maintained and actively looking for maintainers
Example: doorkeeper-openid_connect [meta] Looking for maintainers! #89
The libraries have counter-intuitive behaviours
Example: the example configuration for omniauth_openid_connect does not include an `issuer` field, which leads to a cryptic SSL error. Debugging this required digging 3 layers deep into the dependencies to find the issue above, regarding the hardcoded HTTPS

It’s expected that, for sufficiently advanced use cases, projects end up owning every part of the dependency chain. However, this simple spike required a significant investment digging into the library code, devising monkey patches, or just figuring out how things work in lieu of documentation. This is a strong signal that the tooling requires significant investment.

There is a high risk that DFE-Digital will have to fork and maintain these gems. Alongside being a big time investment, making changes requires deep knowledge of OAuth and OIDC. Moreover, maintaining our own forks also means staying afloat of changes in the specification, as well as new security advisories, CVEs.
Risk #3: Burden to build and ops overhead
It was felt during the spike that there is no “shortcut” to building a quick version of a SSO provider, with good documentation, staging environments, bespoke integrations that might be slightly different for each client, a support team, oncall rota, and so forth.

The closest prior art to a project that tackles the same scope in DFE Digital is DFE Signin. The “gut feeling” after the spike is that there is nothing inherent about our usecase that would make it “easier for us.”

Reaching an MVP state requires a significant investment, with “not much to show” until it is fully done, as a half-baked SSO integration is one half away from being usable.
Alternatives to building a Single Sign-on
In light of the risks discussed above, some alternatives were thought of that would still solve the problems of code reuse / joined up user experience, while avoiding some of the thorny bits of OAuth and OIDC.

# Approach #1: JWT token handover
JSON Web Tokens (JWT) are a secure way of passing messages between two parties. A simple token exchange flow could be implemented both in Find a lost TRN and any client apps that would allow users to transfer data between the services in a secure way, such as their TRNs.

## Quick pros:

Simple to set up, does not require investment into learning the OAuth spec
Working with JWT is a much more common skill in full stack devs, vs building an OIDC provider
Can be packaged as a lightweight gem for clients

## Quick cons:

Will require sharing a private key between multiple projects; any leak would lead to having to perform a full rotation on all connected services
But since we own all these services, that’s reasonable to do; OAuth was specifically designed to mitigate this issue in scenarios where 3rd parties are involved. We don’t have that need, so we don’t get the benefit from using OAuth (but still pay the price in additional complexity)
Need to have a support process to identify and rotate leaked keys

# Approach #2: Packaging up Find a lost TRN as a Rails engine
Rather than passing the user around to a different service, we could package up the core parts of Find into a reusable module that can be plugged into other applications to give them similar functionality as needed.

## Quick pros:

Doesn’t require passing around tokens, or maintaining extra secrets
Find doesn’t become a single point of failure, more distributed

## Quick cons:

# Untried, untested approach
Client apps need to be Rails apps, which would mean we couldn’t integrate Find a lost TRN with legacy Teacher Self-Service Portal for dual-running purposes, for instance
Requires significant development effort from client apps
Distributes the act of “finding a TRN” to multiple apps; makes tracking the total number of requests and performance stats much harder
Approach #3: Building a Single Sign-on on top of another stack, like C#
This involves running another spike but in a different host language that has more mature tooling for dealing with OAuth and OIDC.

## Quick pros:

Alleviates some of the risks around having to fork and own complex open source libraries

#### Quick cons:

Still requires extensive knowledge of OAuth and OIDC, is vulnerable to churn and attrition
Requires implementing Find in C#, or somehow passing messages a’la JWT strategy above; might end up in a situation where we end up building multiple solutions where just one will do
Does nothing to eliminate the ops overhead of running a SSO service
Not likely to be a magnitude less effort than doing this in Ruby
