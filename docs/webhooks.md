# Webhooks

Get an identity uses [webhooks](https://en.wikipedia.org/wiki/Webhook) to push notifications to other services when interesting things happen.

Notifications are sent as JSON and have a common outer schema called the envelope.

```json
{
  "notificationId": "",
  "timeUtc": "",
  "messageType": "",
  "message": {
    //...
  }
}
```

`notificationId` is a unique identifier GUID for this notification. If a notification message is sent multiple times (e.g. when retrying after receiving an error code) this ID will remain consistent.

`timeUtc` is a UTC ISO 8601 timestamp describing when the notification was generated.

`messageType` is a string identifying the type of event that generated the notification. See [message types](#message-types).

`message` is a type-specific object with the details of the notification. Each `messageType` has its own message schema.


## Message types

The are two notification messages types currently; one is for when a user is changed (name, email address etc.) and the other is for when two users are merged.

### `UserUpdated`

`UserUpdated` is generated when a user's email address, name, TRN, TRN lookup status and/or date of birth have been changed, whether by an API call or by the user themselves. It has the following message schema:

```json
{
  "user": {
    "userId": "",
    "emailAddress": "",
    "firstName": "",
    "lastName": "",
    "dateOfBirth": "",
    "trn": "",
    "mobileNumber": "",
    "trnLookupStatus": "None|Pending|Found|Failed"
  },
  "changes": {
    "emailAddress": "",
    "firstName": "",
    "lastName": "",
    "dateOfBirth": "",
    "trn": "",
    "trnLookupStatus": ""
  }
}
```

The `user` object contains the complete set of user information. `dateOfBirth` and `trn` may be `null`.
The `changes` object contains only those properties that were changed and their updated values.

### `UserMerged`

`UserMerged` is generated when two user accounts are merged. When accounts are merged, one is chosen as the master and is retained while the second account is deleted.

The message contains both the user details for the master account and ID of the deleted account. Upon receipt of this webhook, any references to `mergedUserId` (the deleted account)
should be replaced with the ID of the `masterUser`.

```json
{
  "masterUser": {
    "userId": "",
    "emailAddress": "",
    "firstName": "",
    "lastName": "",
    "dateOfBirth": "",
    "trn": "",
    "mobileNumber": "",
    "trnLookupStatus": "None|Pending|Found|Failed"
  },
  "mergedUserId": ""
}
```


## Receiving webhooks

You need a publicly-accessible HTTPS endpoint that accepts JSON using the POST method. Ask one of the Get an identity developers to configure your endpoint.
When the endpoint is configured you will receive a secret; this can be used to [verify the webhook's payload](#verifying-the-webhook).

Your endpoint should return a success status code (200-299) when the webhook has been processed successfully.
If an error code is returned, or the endpoint takes longer than 30 seconds to respond, the message will be retried later. The retry intervals are:
- 30 seconds,
- 2 minutes,
- 10 minutes,
- 1 hour,
- 2 hours,
- 4 hours,
- 8 hours.

If after the final retry the message was still not delivered successfully no further attempts will be made to deliver that message.


## Verifying the webhook

When your endpoint receives a message it should verify that it has been sent by Get an identity.
Each HTTP request includes a header - `X-Hub-Signature-256`. This is an HMAC hex digest of the request body generated using the SHA-256 algorithm using the secret above as the key.
To verify, recalculate this signature and compare it to the header; if the values do not match the message should be disgarded.
