# Create an oidc/oauth server to underpin get an identity

27 July 2022

## Status

Accepted

## Context

Get an Identity service started life as "Teacher Identity" where it went through Discovery and Alpha phases. Although successful, it was clear that technically there many risks in creating an unmanageable highly complex web of software application components. We had identified and accepted (as a cross service intiative) a small set of common goals and objectives. The main ones being:

* Be able to more easily identify our user records and information across our service line (10-12 discreet digital services).
* Better inform policy making and service design by being able to better track a user across services.
* Create better user experiences by reducing the amount of times we ask for core personal data.
* More securely ask for, store, monitor, share and grant access to our users PII data.
* Create better user experiences by being able to use (and link records to) other DfE data sets to gain insights into useage, workforce patterns etc. that allow us to save teachers time and effort in using our services.
* Enable better interventions (by being able to more accurately and reliably identify a user securely); this allows us greater opportunities to automate services such as teacher training incentive payments, automate safeguarding measures etc.
* More easily fix data gaps and errors.

There are many ways to implement this kind of data integration and single sign on functionality. There are indeed a number of GOV solutions in this space. We have considered using:

* [DfE sign](https://services.signin.education.gov.uk/). This service doesn't meet our use case because it is DfE Sign-in is how schools and other education organisations access DfE online services. Our users access our services as individuals outside of an educational organisation e.g. apply for a job, apply for initial training are not services where an organisation is relevant.

* [Gov Sign](https://www.sign-in.service.gov.uk/). We do intent to integrate and leverage this service. However, it doesn't alone provide us with sufficient coverage of our use cases in order to achieve our goals and objectives. It is also not in a state (at the time of writing) where we can on board sufficiently as to provide benefit. We have complete a successful [GOV Sign technical spike POC](https://github.com/DFE-Digital/get-an-identity/tree/main/openid_connect_poc) where we integrated a dummy Ruby web application (representing one of our digital services) with the GOV Sign service. We feel we can extend our service later in order to provide:
    * An alternative trusted GOV identity
    * Better identity verification (using the GOV sign Identity wallet service)

We also considered:

* Point to point service integration, by simply extending our internal API architecture.

    There is mileage in extending our internal API architecture and we feel this will be part of the overall technical architecture of "Get and Identity". However, to reduce complexity (of managing version dependency) as well as wanting to take the opportunity of improving our data integration security we feel this does provide the best solution on its own.

* Eventing / Service Bus integration

    Again, we may utilise a pub/sub mechanism within the overall technical architecture. We feel it may help us mitigate against the risk of becoming a single point of failure, especially around record checking / matching in the Database of Qualified teachers, where its underlying technology places fairly tight rate limiting against the data set which we could start to hit.

* Using an off the shelf identity provider

    Although we haven't to date completed a technical spike or POC around an Auth0/Okta A.N.Other 3rd party provider, we felt as a technical team that our past experiences in integrating with such services and knowledge of our intended goals and use cases led us to believe that it would be more complex, expensive and more importantly limit our ability to meet our users needs efficiently. We felt we would end up writing a custom wrapper around a SAAS service. We also felt that the open source .Net libraries provided what we need from an Authentication and Authorisation server and aligns better to our ways of working and adherence to GDS principles of using open source and open standards by default.


## Decision

We will create a digital service underpinned by the OAUTH/OIDC protocols. We will be frugal in the way we architect the core server, being careful not to make it overly complex and iterate its code base over time, as opposed to build everything up front. We will look to use open source and open standard based boiler plate libraries as much as possible. 

## Consequences

* The service will become a dependency and potential single point of failure to Teacher Services digital services that we will need to manage carefully.
* We will need to upskill developer knowledge around OIDC/OAUTH protocols.


