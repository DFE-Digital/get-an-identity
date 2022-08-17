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
        authz_server->>find_lost_trn: POSTs user to /identity with a callback URL + additional context
        find_lost_trn->dqt_api: resolve TRN
        find_lost_trn->authz_server: calls API to store the user information for the journey ID
        find_lost_trn->>authz_server: redirects user to callback URL
        authz_server->dqt_api: persist the teacher_id against the DQT contact record
    end
    authz_server->>client: redirects user to the client with id_token containing email + TRN claims
```


## Handover from authorization server to Find a lost TRN

When the authorization server needs a TRN for the user it POSTs to the Find a lost TRN service's `/identity` endpoint using the `application/x-www-form-urlencoded` content type.
This request includes some additional context, specified as form values:

| Query parameter | Remarks |
| --- | --- |
| email | The verified email address as captured by the authorization server. This allows Find a lost TRN to skip asking the user for their email address again. |
| redirect_uri | This is the callback URL on the authorization server that Find should redirect the user to once it has resolved a TRN. |
| client_title | The name of the client that initiated the authorization journey. This enables 'branding' both the the authorization server and Find a lost TRN such that the user perceives the journey as a single service e.g. 'Register for a National Professional Qualification'. |
| journey_id | A unique ID for this authorization journey instance. |
| sig | This is a signed hash of the previous parameters using a pre-shared key in hexadecimal format. |

### Context signature parameter

Since the context data above is sent over the user's browser (the 'front channel' in OAuth terms) it is necessary to include some mechanism by which Find a lost TRN can verify that the data has not been tampered with and did indeed come from the authorization server. This signature is that mechanism.

In essence, the form parameters above (except 'sig') are hashed using HMAC SHA256 with a secure key known to both the authorization server and to Find a lost TRN. When it receives a request, Find a lost TRN should re-compute the signature and compare it to the signature it was passed. If the signatures match, Find a lost TRN can assume the request has not been tampered with. If the signatures do not match, it should reject the request and show an error.

#### Computing the signature

1. Take all the form parameters, sort them alphabetically by key and remove the 'sig' parameter. N.B. For forward compatibility it's important that every parameter is included, not just the known ones.
2. Encode each parameter as a key-value pair, separated by `&` with a `=` between the key and the value. Keys and values should be [percent encoded](https://developer.mozilla.org/en-US/docs/Glossary/percent-encoding).
3. Hash the combined key-value pairs using the secure pre-shared key and the HMAC SHA256 algorithm.

##### Example:
Given a pre-shared key of `qNhFcrwurK5Rf9qJeH7KaU3F`
and POSTed request:
```
POST /identity HTTP/1.1
Host: https://find-a-lost-trn.education.gov.uk
Content-Type: application/x-www-form-urlencoded

redirect_uri=https%3A%2F%2Fauthserveruri%2F&client_title=The%20Client%20Title&email=joe.bloggs@example.com&journey_id=9ddccb62-ec13-4ea7-a163-c058a19b8222&sig=D3E09A334FAD2201A88C7199250C9670C368102B3581A5DBF9C76F503B8A0575`
```

1. Sort parameters and remove 'sig':
   | Parameter name | Value |
   | --- | --- |
   | client_title | The Client Title |
   | email | joe.bloggs@example.com |
   | journey_id | 9ddccb62-ec13-4ea7-a163-c058a19b8222 |
   | redirect_uri | https://authserveruri/ |
2. Encode parameters and combine:\
    `client_title=The%20Client%20Title&email=joe.bloggs@example.com&journey_id=9ddccb62-ec13-4ea7-a163-c058a19b8222&redirect_uri=https%3A%2F%2Fauthserveruri%2F`
3. Sign the string with the PSK\
    `D3E09A334FAD2201A88C7199250C9670C368102B3581A5DBF9C76F503B8A0575`


## Handover from Find a lost TRN to the authorization server

Once Find a lost TRN has completed its journey (whether successfully resolving a TRN or not), it needs to provide the user information to the authorization server and redirect back there.

Find a lost TRN must do a back-end API call to a PUT endpoint on the authorization server's API `/api/find-trn/user/{journeyId}` (where `{journeyId}` is the ID passed in the initial handover request).
The body is a JSON object with a single property - `subject`. This should contain a JWT that includes the user's names, DOB, NINO and TRN, if known. The JWT does not need to be signed.

Finally, Find a lost TRN should redirect the user to the `redirect_uri` specified in the initial handover request.

When the authorization server receives the callback, it will look up the user information that was persisted against the journey ID by the API call. Using that information it will register the user and complete the authorization process.

### Example API call:

```
PUT /api/find-trn/user/2345 HTTP/1.1
Host: https://authserveruri
Content-Type: application/json

{
  "user": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJnaXZlbl9uYW1lIjoiRmlyc3ROYW1lIiwiZmFtaWx5X25hbWUiOiJMYXN0TmFtZSIsInRybiI6IjEyMzQ1NjciLCJiaXJ0aGRhdGUiOiIxOTkwLTAxLTAxIiwibmlubyI6IlFRMTIzNDU2QyIsIm5iZiI6MTY1OTYxMzUzMywiZXhwIjoxNjU5NjE3MTMzLCJpYXQiOjE2NTk2MTM1MzN9.MZSUNq2jG1loFD0X5H76pMmNnN-CMLjAMdCqBrvYz_8"
}
```