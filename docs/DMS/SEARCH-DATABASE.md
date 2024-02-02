# DMS Feature: Read-only Search Database

Braindump:

* Initially Elasticsearch and OpenSearch
  * Attempt to build a single plugin that will work with both.
* Populated via streaming
  * Consider alternate: post directly to the streaming database in the
    transaction context. Slower API response, but eliminates management of the
    streaming platform. Is that a compromise that anyone would willingly make?
* Consider case sensitivity carefully.
