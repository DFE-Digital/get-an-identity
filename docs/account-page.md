# Account page

The Account page in Identity is built so that users can navigate from a client service to the account page and back again seemlessly.

For this to work, the account page needs some additional context passing to it so that it knows which service linked to it.
That context is provided as three query parameters, detailed below:

## `client_id`
This is the same `client_id` as used in the OAuth sign in request.

## `redirect_uri`
The fully-qualified URL to send a user back to when they're done on the account page.
This domain portion of this `redirect_uri` must match the domain portion of a pre-configured `redirect_uri` in ID for the client.
(Note that unlike the `redirect_uri` provided for an OAuth request, this does not have to match exactly.)

## `sign_out_uri`
The fully-qualified URL to send a user to that begins the sign out process. It's important that the sign out process signs the user
out of both the client and Identity. (See [the OIDC spec](https://openid.net/specs/openid-connect-rpinitiated-1_0.html) for more information.)
This domain portion of this `sign_out_uri` must match the domain portion of a pre-configured `redirect_uri` in ID for the client.
(Note that unlike the `redirect_uri` provided for an OAuth request, this does not have to match exactly.)


## Example URL
`https://preprod.teaching-identity.education.gov.uk/account?client_id=testclient&redirect_uri=https%3A%2F%2Fs165t01-getanid-preprod-testc-app.azurewebsites.net&sign_out_uri=https%3A%2F%2Fs165t01-getanid-preprod-testc-app.azurewebsites.net%2Fsign-out`