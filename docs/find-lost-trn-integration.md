# Find a lost TRN integration with the authorization server

When the `trn` scope is requested by a client, the authorization server needs to integrate with the
Find a lost TRN service in order to resolve a DQT contact record (via a TRN). The details of how data is handed over
from the authorization server to Find and back again are listed below.


```mermaid
sequenceDiagram
    participant client
    participant authz_server
    participant find_lost_trn
    participant dqt_api
    client->>authz_server: redirects user to the authorization endpoint with 'trn' scope
    authz_server->>authz_server: prompts user for email
    alt existing user
        authz_server->dqt_api: lookup DQT contact record with matching teacher_id
    end
    alt not existing user or lookup failed
        authz_server->>find_lost_trn: redirects user with a callback URL + additional context
        find_lost_trn->dqt_api: resolve TRN
        find_lost_trn->>authz_server: redirects user to callback URL with JWT containing TRN
        authz_server->dqt_api: persist the teacher_id against the DQT contact record
    end
    authz_server->>client: redirects user to the client with id_token containing email + TRN claims
```


## Handover from authorization server to Find a lost TRN

When the authorization server needs a TRN for the user it redirects to the Find a lost TRN service.
This redirect includes some additional context, specified as query parameters:

| Query parameter | Remarks |
| --- | --- |
| email | The verified email address as captured by the authorization server. This allows Find a lost TRN to skip asking the user for their email address again. |
| redirect_uri | This is the callback URL on the authorization server that Find should redirect the user to once it has resolved a TRN. |
| client_title | The name of the client that initiated the authorization journey. This enables 'branding' both the the authorization server and Find a lost TRN such that the user perceives the journey as a single service e.g. 'Register for a National Professional Qualification'. |
| sig | This is a signed hash of the previous query parameters using a pre-shared key. |

### Context signature query parameter

Since the context data above is sent over the user's browser (the 'front channel' in OAuth terms) it is necessary to include some mechanism by which Find a lost TRN can verify that the data has not been tampered with and did indeed come from the authorization server. This signature is that mechanism.

In essence, the query parameters above (except 'sig') are hashed using HMAC SHA256 with a secure key known to both the authorization server and to Find a lost TRN. When it receives a request, Find a lost TRN should re-compute the signature and compare it to the signature it was passed. If the signatures match, Find a lost TRN can assume the request has not been tampered with. If the signatures do not match, it should reject the request and show an error.

#### Computing the signature

1. Take the query string from the incoming request and remove the leading '?' and the entire 'sig' query parameter.
2. Hash what remains using the secure pre-shared key and the HMAC SHA256 algorithm.

### Example:
Given a base64-encoded pre-shared key of `L8mg9SK6ymyvwTQ4j0kl6rIxQv4wR3XWFcD8ctMrk4w=`

`https://find-a-lost-trn.education.gov.uk/?redirect_uri=https%3A%2F%2Fauthserveruri%2F&client_title=The%20Client%20Title&email=joe.bloggs@example.com&sig=s8uqqS8kQdKrByoKNZIka%2FlvpRn2FqHQo6dt2HIm9qM%3D`

TODO: Do we want a different endpoint on find for these type of requests?


## Handover from Find a lost TRN to the authorization server

Once it's found a TRN, Find a lost TRN needs to redirect back to the authorization server with that TRN.
The `redirect_uri` specified in the initial request to Find a lost TRN should have an additional query parameter appended - `user`. The `user` query parameter should contain a JWT that includes the TRN (TODO: plus more data - names, DOB?). The JWT should be signed with the same pre-shared key referenced above.

When the authorization server receives the callback, it should validate the JWT has been signed by the pre-shared key. If validation fails an error should be shown; the authorization process must *not* complete.

### Example:

`https://authserveruri/?user=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJnaXZlbl9uYW1lIjoiRmlyc3ROYW1lIiwiZmFtaWx5X25hbWUiOiJMYXN0TmFtZSIsInRybiI6IjEyMzQ1NjciLCJuYmYiOjE2NTkxMDYyOTksImV4cCI6MTY1OTE5MjY5OSwiaWF0IjoxNjU5MTA2Mjk5LCJpc3MiOiJodHRwczovL2ZpbmQtYS1sb3N0LXRybi5lZHVjYXRpb24uZ292LnVrLyIsImF1ZCI6Imh0dHBzOi8vYXV0aHNlcnZlcnVyaS8ifQ.9NbHCIP6jqqFQBvbrZkRNpq22Tv2mwEbcHY9QzNo6Wg`


TODO: If Find cannot resolve a TRN should it still redirect back to the authorization server?