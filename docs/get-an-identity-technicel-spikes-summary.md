# Get an Identity Technical Spikes Summary

July 2022

This is a summary of the technical spikes we undertook when considering the technical architecure and application technology and software design of the "Get an Identity" service.

## ADR's 

We have used [ADR (Architecture Decision Record) templates](https://github.com/joelparkerhenderson/architecture-decision-record) to record our technical architecture decisions arising from the spikes mentioned in this summary.

Our [ADR's](/docs/architecture/decisions/) to date

## Early tech spike

Questions we were trying to answer: 
* Do we need to custom build anything? 
* How do we (and when) can we integrate to [Gov Sign](https://www.sign-in.service.gov.uk/) ?

The answers being, "yes" we need to build something and "yes" we can successfully integrate a dummy service. Details and justifications can be found in the [relevant ADR's](/docs/architecture/decisions/0004-create-an-oidc-oauth-server-for-get-an-identity.md) and by running the [Gov Sign OIDC Proof of Concept application](/openid_connect_poc/).

## Tech spikes related to building our own Auth Server.


Given our descision to underpin "Get an Identity" service by [building an authentication server using OIDC/OAUTH protocols](/docs/architecture/decisions/0004-create-an-oidc-oauth-server-for-get-an-identity.md) we wanted to test some hypothesis that we had started to create:


* The available Ruby Gems are good enough to support our expected technical needs
* OAUth/OIDC flows are a good basis to support our user journeys [as per our interaction design histories](https://tra-digital-design-history.herokuapp.com/get-an-identity/)

## Initial findings

While the spike to build “Get an identity” as a Single Sign-on (SSO) provider based on OAuth v2.0 and OpenID Connect was successful, it uncovered a number of significant risks to taking this approach. Full write up can be found [in the tech spikes write up folder](/docs/tech-spike-write-ups/get-an-identity-oidc-oauth-with-ruby)

Based on the risks identified in the first spike we initiated another spike to test if switching the auth server technology would provide sufficient mitigation to the risks we found.

## Conclusions and ADR's

We found that we could build our test application much more easily using .Net. The [OpenIddict library](https://github.com/openiddict) provided the functionality we required with very little effort. It was clear that the libraries are actively maintained. We also managed to test that we could support the interaction design history for our [first service integration](https://tra-digital-design-history.herokuapp.com/get-an-identity/). Full write up can be found [in the tech spikes write up folder](/docs/tech-spike-write-ups/get-an-identity-oidc-oauth-withdotnet.md).

We therefore made the [decision](/docs/architecture/decisions/0005-use-dotNet-C%23-for-get-an-identity-auth-server.md) to continue to base the technical architecture of "Get an Identity" on an OAUth/OIDC server pattern and to use .Net C#. 

