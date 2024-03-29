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
| redirect_url | This is the callback URL on the authorization server that Find should redirect the user to once it has resolved a TRN. |
| client_title | The name of the client that initiated the authorization journey. This enables 'branding' both the the authorization server and Find a lost TRN such that the user perceives the journey as a single service e.g. 'Register for a National Professional Qualification'. |
| client_url | The home page of the calling service, used to generate the header link. |
| previous_url | The URL of the page that POSTed to Find, used to generate a back link. |
| journey_id | A unique ID for this authorization journey instance. |
| session_id | An ID for the session used for analytics (if specified by the calling client). |
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

redirect_url=https%3A%2F%2Fauthserveruri%2F&client_title=The%20Client%20Title&email=joe.bloggs@example.com&journey_id=9ddccb62-ec13-4ea7-a163-c058a19b8222&client_url=https%3A%2F%2Fcalling.service.gov.uk&previous_url=https%3A%2F%2Fauthserveruri%2Fsign-in%2Ftrn&sig=f8aafaee18726270ddffa71768c59a954cc66e3b2b86fb41a181fffcfc589259`
```

1. Sort parameters and remove 'sig':
   | Parameter name | Value |
   | --- | --- |
   | client_title | The Client Title |
   | client_url | https://calling.service.gov.uk |
   | email | joe.bloggs@example.com |
   | journey_id | 9ddccb62-ec13-4ea7-a163-c058a19b8222 |
   | previous_url | https://authserveruri/sign-in/trn |
   | redirect_url | https://authserveruri/ |
   | session_id | somesessionid |
2. Encode parameters and combine:\
    `client_title=The%20Client%20Title&client_url=https%3A%2F%2Fcalling.service.gov.uk&email=joe.bloggs%40example.com&journey_id=9ddccb62-ec13-4ea7-a163-c058a19b8222&previous_url=https%3A%2F%2Fauthserveruri%2Fsign-in%2Ftrn&redirect_url=https%3A%2F%2Fauthserveruri%2F`
3. Sign the string with the PSK\
    `f8aafaee18726270ddffa71768c59a954cc66e3b2b86fb41a181fffcfc589259`


## Handover from Find a lost TRN to the authorization server

Once Find a lost TRN has completed its journey (whether successfully resolving a TRN or not), it needs to provide the user information to the authorization server and redirect back there.

Find a lost TRN must do a back-end API call to a PUT endpoint on the authorization server's API `/api/find-trn/user/{journeyId}` (where `{journeyId}` is the ID passed in the initial handover request).
The body is a JSON object containing `firstName`, `lastName`, `dateOfBirth` and `trn` properties.

Finally, Find a lost TRN should redirect the user to the `redirect_url` specified in the initial handover request.

When the authorization server receives the callback, it will look up the user information that was persisted against the journey ID by the API call. Using that information it will register the user and complete the authorization process.

### Example API call:

```
PUT /api/find-trn/user/2345 HTTP/1.1
Host: https://authserveruri
Content-Type: application/json
Authorization: Bearer your_api_key

{
  "firstName": "Joe",
  "lastName": "Bloggs",
  "dateOfBirth": "1990-04-20",
  "trn": "1234567"
}
```