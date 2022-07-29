# Teacher Services Get an Identity formerly Teacher ID - part of TS Data Transformation

28 July 2022

## Brief History
Last year we agreed to deliver a cross service program to address some common issues and needs across our service lines. These included addressing data gaps hindering policymaking, data quality and data duplication issues.

Right now; the only form of “identity” is the Teacher reference number (TRN). This has a number of problems uncovered in the "TRN Discovery" of 2019. The [TRN holders design history post](https://tra-digital-design-history.herokuapp.com/teacher-self-service-portal/trn-holders/) explains the future direction of TRN use across our services. Get an Identity service will help in its implementation.

## First Iteration
We’ve launched a new service, Find a lost TRN, this allow teachers (and trainees) to find their TRN, enabling them to access other services, such as Teacher Pensions and CPD (The TRA Helpdesk had 18,000 enquires/year with users looking to find their TRN).

This service takes their personal identifiable information and completes a fuzzy match in Databse of Qualified Teachers (DQT) to return their TRN.

## What's Next?
The next phase will see us extend and scale this to remove the reliance on TRN and the DQT so that we can allow users to create an identity we can track across services. We will also extend the cohort to include non teachers (applicants / participants). The service we are aiming to deliver to enable this is “Get an Identity”.

## Key Features
“Get an identity” will be a shared component across DfE Teacher Services. It will...

* Allow trainee teachers and teachers to identify themselves across all teacher services with a single set of credentials.
* Allow those trainee teachers and teachers to tie their credentials to their DfE records
* Enable us to more easily produce a single view of those users and join up their records across multiple services.

More information can be found in the [service’s design history]((https://tra-digital-design-history.herokuapp.com/)).

## Challenges
Some of the challenges we face are…

* Team transition: We are currently forming a new program and team structure, splitting the existing TRA digital team into a number of discrete product teams.

* Maintaining a good user experience: All our services have invested heavily in user needs. This service has to be as transparent as possible to current journeys.

* Delivery disruption: Being the first transactional cross service program of this scale we are mindful that each service has their own delivery commitments and deadlines. Inevitably we will be impacting service backlogs. This will be challenging but we are confident that we can work together to reduce disruption to an absolute minimum.

* Technology: Early in our [technical spikes](/docs/get-an-identity-technicel-spikes-summary.md) we have identified some technological challenges to our initial tech stack choice. We intend to base the underpinning service on the creation of an authentication / authorisation server using open standard protocols (OAUTH/OIDC). We have made the decision to use .Net libraries (as opposed to Ruby) following a number of tests and technical spikes. This will not affect us building our services in Ruby though, in any way.

* Integrating existing service accounts: Some of our services have existing account / login functionality.

* Integrating to existing single sign in solutions: We already have DfE Sign in (for organisations), and GDS are currently building GOV.UK Sign In. DfE sign caters for different use cases (external organisations). We are using the same protocols as GOV.UK Sign and will be working with them to offer users a choice of sign in providers.

* Not creating a monster: Creating a cross service line service of this nature could get out of hand quickly. We are mindful this needs to be as simple and manageable as possible.

## Roadmap (TBC):

* NPQ
* Teacher Self Serve (allow teachers to update their information, download QTS and NPQ certs - more user needs to be uncovered)
* Apply for QTS
* Claim
* Teaching vacancies
* Apply for teacher training
* Find teacher training

## Very High Level Design

THis shows a very high level indicitive view of how "Get an identity" will work:

![](/docs/images/get-an-identity-v-hld.jpg)



