# Importing users into identity

## Overview

As more and more DfE services migrate to using `Identity` to manage authentication and authorisation, there is the need to migrate existing users of these services into identity.  
While this could be done the next time the user interacts with the specific service, it would mean them having to re-enter their details in order to create a teaching account.  
The user import feature has been created to automate the creation of teaching accounts in identity from users of other DfE services as much as possible.

## Import File Definition

The user import requires a CSV file with a header with the following fields:

| Header Name   | Description                                                  | Mandatory? | Expected Format                                    |
| ------------- | ------------------------------------------------------------ | -----------| -------------------------------------------------- |
| ID            | The unique ID associated with the user in the source service | Mandatory  | A string up to 100 characters                      |
| EMAIL_ADDRESS | The user's email address                                     | Mandatory  | A valid email address format up to 200 characters* |
| FIRST_NAME    | The user's first name                                        | Mandatory  | A string up to 200 characters                      |
| LAST_NAME     | The user's last name                                         | Mandatory  | A string up to 200 characters                      |
| DATE_OF_BIRTH | The user's date of birth                                     | Mandatory  | A valid date in ddMMyyyy format e.g. 03051971      |
| TRN           | The user's TRN (if known in the source service)              | Optional   | Empty or a 7 digit number                          |

\* Note that there is no validation of whether the email address supplied is actually a valid personal email

## Download File Definition

The results of each file import can be downloaded in a CSV file with the following fields:

| Header Name                 | Description                                                  | Format                                                 |
| --------------------------- | ------------------------------------------------------------ | ------------------------------------------------------ |
| ROW_NUMBER                  | The row number from the original uploaded CSV file           | An integer                                             |
| ID                          | The unique ID associated with the user in the source service | A string                                               |
| USER_ID                     | The unique ID associated with the user in identity           | A GUID                                                 |
| USER_USER_IMPORT_ROW_RESULT | The outcome associated with the row of data from the CSV     | One of `None`, `UserAdded`, `UserUpdated` or `Invalid` |
| NOTES                       | Any notes e.g. errors                                        | A string with multiple notes separated by ". "         |
| RAW_DATA                    | The raw row of data from the original uploaded CSV file      | A string                                               |

This CSV can then be used by the source service to enhance its data with user IDs from Identity

## User Import Processing

The following diagram shows how each row in the CSV file is processed and the possible outcomes:

```mermaid
flowchart TD
    rowdata[Process CSV Row] --> format{Are fields in valid format?}
    format -- Yes --> emailmatch{Is there an existing user<br/>with the same email address?}
    format -- No --> invalid[Invalid - user data not updated]
    emailmatch -- Yes --> trn{Is the TRN supplied in the CSV?}
    trn -- No --> none[Nothing to do - user data not updated]
    none --> setuserid[Update result data with user Id]
    trn -- Yes --> trnmissing{Is the TRN missing<br/>for the existing user record?}
    trnmissing -- Yes --> trninuse{Is there already another user<br/> with the same TRN?}
    trninuse -- No --> updatetrn[(Update TRN for existing user)]
    trninuse -- Yes --> invalid
    updatetrn --> setuserid
    trnmissing -- No --> trnmatch{Is the TRN in the CSV different<br/>to the existing identity user?}
    trnmatch -- No --> none
    trnmatch -- Yes --> invalid
    emailmatch -- No --> fuzzy{"Is there an existing user<br/>with the same<br/>first name (or a synonym),<br/>last name and<br/>date of birth?"}
    fuzzy -- Yes --> potdup[Potential Duplicate]
    potdup --> invalid 
    fuzzy -- No --> existingtrn{Is there an existing user<br/>with the same TRN?}
    existingtrn -- Yes --> invalid
    existingtrn -- No --> insert[(Add a new user)]
    insert --> setuserid
```
