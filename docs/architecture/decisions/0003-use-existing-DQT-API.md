# 1. Record architecture decisions

Date: 2021-12-13

## Status

Accepted

## Context

We need a way of accessing data in the DQT - there are a few options.  Either integrate with the DQT directly, create an new API or use the existing DQT API (v3) by using/extending its functionality.

## Decision

We will use the existing DQT API, and extend it where necessary to support the features of the Find My TRN service

## Consequences

* We dont have to write an API from scratch
* We are dependant on the DQT API team to make any changes

