# Use .Net C# to implement the auth server components of get an identity

27 July 2022

## Status

Accepted

## Context

We completed 2 technical spikes to understand what technology was the best fit to use to implement the auth server engine
of "get-an-identity" service. We wanted a set of libraries in order to provide the boiler plate code of OIDC/OAUTH 2.0 protocols
on which we had made the decision to base the auth server components of the application (see ADR 0004). 

Spike 1: Using Ruby

https://github.com/DFE-Digital/find-a-lost-trn/pull/265
https://github.com/DFE-Digital/find-a-lost-trn/pull/171

Spike 2: Using .Net

https://github.com/DFE-Digital/get-an-identity/tree/main/dotnet-authserver

## Decision

We will use .Net C# to implement the auth server components of "Get an Identity". We feel that there is less risk overall (despite not being our number 1
language of choice, we do have othe .Net code within teacher services). The main factor being the libraries available in C# are much more plentiful and mature. In Ruby there is (at the time of writing) only a few [Gems](https://github.com/DFE-Digital/find-a-lost-trn/pull/265) supporting OIDC/OATH. We tested device and dooekeeper. It took more effort than we thought reasonable to get a simple test app working, our tech lead raised a number of risks and concerns during coding. A particular risk was the fact the library did not seem to be very actively maintained. While it supported the main OAUth/OIDC flows, its support wasn't as comprehensive as we founf in spike 2 (.Net).

 In contrast, the [.Net libraries](https://github.com/openiddict) we found to be actively maintained and comprehensive in their support for the protocols, with a full set of samples that could (and were) very easily set up to create our test apps. Risks highlighted in the [Get an identity tech spike summary](/docs/get-an-identity-technicel-spikes-summary.md) around knowledge gaps and steep learning curve could also be mitigated by the fact our .Net tech lead has used these libraries before on a number of occasions and therefore, can be verified in our team to "work well".

For a full report see [Get an identity tech spike summary](/docs/get-an-identity-technicel-spikes-summary.md).


## Consequences

* We will need to support .Net C# (which we already do) although in Teacher Services it is second to Ruby
* We will need to manage any dependencies between our code base and https://github.com/openiddict