# Ed-Fi Data Management Service Features and Design

## Features

This list of features is intended to help create and prioritize epics and user
stories for the software development process. The list is organized in three
initial categories: preview release (fall 2024), minimum viable production
release (2025), and other features that may be added in the future. This grouping
of features is subject to further revision based on feedback.

### Preview Release

* [Resources API](./RESOURCES-API.md)
* [Descriptors API](./DESCRIPTORS-API.md)
* [Primary Data Storage](./PRIMARY-DATA-STORAGE.md)
* [Cascading Key Updates](./CASCADING-UPDATES.md)
* [Change Data Capture to Stream](./CDC-STREAMING.md)
* [Read-only Search Database](./SEARCH-DATABASE.md)
* Configuration Retrieval and Caching
* Authentication and Authorization

While the streaming and read-only capabilities are not strictly necessary, it is
important that we have early progress toward these motivating goals for Project
Tanager.

### Production Release

* Extension Support
* Profile Support
* Change Queries API
* Multi-tenancy
* Optimistic Concurrency (etags)

### Future Consideration

* Identity API
* Link Objects
* Assessment API (not in the form of the ODS/API's Composites)
* Assessment Rostering API

## Design Notes

Rough notes on other design topics that might not fit into the Features above.
These may be expanded out into more detailed documents when timely.

* Plugin architecture for backend support
  * .NET plugin model for drop-in support.
  * Clean and separated interfaces for reads and writes.
  * Minimal business logic: focus on managing interactions with the data store.
  * Transactions handling should be considered as business logic.
    * Implication: anyone building a plugin for a database that does not have
      traditional transaction support would still need to implement the
      interface with no-op functions.
* Need to review case sensitivity and JSON serialization in detail.
* Review and implement new error message formatting that is being applied in
  ODS/API 7.2.
