# DMS Feature: Including Metadata in GET Responses

Short version:

* `_lastModifiedDate`
* `_etag`

into the document body, so that streamed data looks like the API.

Eventually lineage will be the same.

Also talk about allowing queries based on `lastModifiedDate` (without
underscore). But how would we determine direction? The only point of a query by
datetime is be less than or greater than (or equal) to the given value.

`?modifiedBefore=` and `?modifiedAfter` perhaps.
