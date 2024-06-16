---
title: Configuration Service
---

# Ed-Fi Configuration Service Features and Design

:::note

Braindump:

* Supports Admin API specification (version 2)
* Consider expansion to handle tenant registration and/or creation (replace
  Sandbox Admin?). New API specification.
* Consider cache management. Should an API go here or in the Data Management
  Service? Maybe DMS via a webhook?
  * On claimset definition change, need to propagate update to DMS cache. POST a
    command to the DMS with a simple webhook, it then initiates retrieval of
    full claimset information.
  * Is that sufficiently secure?
* Need sys admin authorization.
* Must support key rotation procedure

:::
