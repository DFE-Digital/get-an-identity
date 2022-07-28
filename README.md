# Get An Identity

A service that allows trainee teachers, applicants, praticipants and teachers to get an identity that they can use across
DfE Teacher Services digital services and allows the DfE to provide better user experiences by joining user data across the services
to prevent re-keying details.

## Environments
Information coming soon...

### Useful Links

| Name           | URL                                                                                                            |
| -------------- | -------------------------------------------------------------------------------------------------------------- |
| Design History | [get-an-identity](https://tra-digital-design-history.herokuapp.com/get-an-identity/)                 |
| Tech. Arch.    | [high-level-technical-design](/docs/images/get-an-identity-v-hld.jpg)
| ADR'S          | [technical-decisions](/docs/architecture/decisions/) |
| Tech Docs      | [tech-docs-site](https://teacher-services-tech-docs.london.cloudapps.digital/)
| TRN Design     | [trn-design-history](https://tra-digital-design-history.herokuapp.com/teacher-self-service-portal/trn-holders/)
### Details and configuration

| Name           | Description                                   | Status
| -------------- | --------------------------------------------- | -------------- |
| Production     | Public site                                   | Coming Soon    |
| Pre-production | For internal use by DfE to test deploys       | Coming Soon    |
| Test           | For external use to test                      | Coming Soon    |
| Development    | For internal use by DfE for dev.              | Coming Soon    |

## Dependencies

- TBC

## How the application works

The application will provide an authentication server for teacher services digital applications (clients) to use in order to provide
an "account" style function (where needed), authentication and authorisation flows based on OIDC/OATH 2.0 protocols. It will define
and make available to clients a set of data scopes, containing data claims relating to the end user. The service will create and store a
unique identifier for the end user. This identifier can be used by clients to create a foreign key data relationship for that user allowing us to
more easily tie data sets together (without using TRN's), in order to provide better data insights for more effective policy making,
better interventions on behalf of our users and address data errors, gaps and duplications. As well as providing a transactional level
service we will also hook into our analytical data stores to enable more accurate and effieicent metrics and reporting at user level.

We keep track of architecture decisions in [Architecture Decision Records
(ADRs)](/adr/).

## Setup

Information coming soon...

### BigQuery

Information coming soon...

### Intellisense

Information coming soon...

### Linting

Information coming soon...

### Testing

Information coming soon...

### Ops manual

Information coming soon...

## Licence

[MIT Licence](LICENCE).
