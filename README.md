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
| Tech. Arch.    | [high-level-technical-design](/docs/images/get-an-identity-v-hld.jpg) |
| ADR'S          | [technical-decisions](/docs/architecture/decisions/) |
| Tech Docs      | [tech-docs-site](https://teacher-services-tech-docs.london.cloudapps.digital/) |
| TRN Design     | [trn-design-history](https://tra-digital-design-history.herokuapp.com/teacher-self-service-portal/trn-holders/) |

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
an "account" style function (where needed), authentication and authorisation flows based on OIDC/OAUTH 2.0 protocols. It will define
and make available to clients a set of data scopes, containing data claims relating to the end user. The service will create and store a
unique identifier for the end user. This identifier can be used by clients to create a foreign key data relationship for that user allowing us to
more easily tie data sets together (without using TRN's), in order to provide better data insights for more effective policy making,
better interventions on behalf of our users and address data errors, gaps and duplications. As well as providing a transactional level
service we will also hook into our analytical data stores to enable more accurate and effieicent metrics and reporting at user level.

We keep track of architecture decisions in [Architecture Decision Records
(ADRs)](/adr/).

## Setup

### Developer setup

#### Software requirements

The API is an ASP.NET Core 7 web application. To develop locally you will need the following installed:
- Visual Studio 2022 (or the .NET 7 SDK and an alternative IDE/editor);
- a local Postgres 13+ instance;
- [SASS]( https://sass-lang.com/install).

#### Initial setup

Install Postgres then add a connection string to user secrets for the `TeacherIdentityServer`, `TeacherIdentityAuthServerTests` and `TeacherIdentityServerEndToEndTests` projects.

```shell
dotnet user-secrets --id TeacherIdentityServer set ConnectionStrings:DefaultConnection "Host=localhost;Username=your_postgres_user;Password=your_postgres_password;Database=teacher_identity"
dotnet user-secrets --id TeacherIdentityAuthServerTests set ConnectionStrings:DefaultConnection "Host=localhost;Username=your_postgres_user;Password=your_postgres_password;Database=teacher_identity_tests"
dotnet user-secrets --id TeacherIdentityServerEndToEndTests set AuthorizationServer:ConnectionStrings:DefaultConnection "Host=localhost;Username=your_postgres_user;Password=your_postgres_password;Database=teacher_identity_e2etests"
```
Where `your_postgres_user` and `your_postgres_password` are the username and password of your Postgres installation, respectively.

Next set the email address you want to use for local development.

```shell
dotnet user-secrets --id TeacherIdentityServer set DeveloperEmail "your_email_address"
```
Where `your_email_address` is an email address you can access.

Finally run the DevBootstrap project. This will create the database, add an admin user account and configure the OIDC clients for local development.

#### Test client

The solution includes a test client that is used for testing end-to-end OIDC flows.
Once initial setup has been done, you should be able to launch both the `TeacherIdentity.AuthServer` project and the `TeacherIdentity.TestClient` project and browse to `https://localhost:7261` to start the sign in journey and sign in with the email address your configured above.


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
