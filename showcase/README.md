# Project Tanager Showcase

This folder contains "showcases" with architectures, prototypes, and
proof-of-concepts of how to extend the Data Manager Service Platform
architecture to satisfy new and interesting requirements.

> [!TIP]
> Warmly welcoming pull requests from community contributors! Suggested
> format for showcases:
>
> * Use Case or Objective
> * Technical Requirements
> * Architecture
> * (Optional) Proof of Concept (code and discussion)

## Deployment

The DMS source code includes Docker Compose files and associated scripts that
orchestrate deployment on a localhost environment. Docker Compose is not
typically used in production environments, since most container deployments
benefit from advanced auto-scaling, load balancing, and health monitoring
facilities. In theory, it could serve well for static hosting situations
equivalent to traditional on-premises installations. Most deployments to
production or production-like systems will use techniques other than Docker
Compose.

Showcases:

* [On-premises Deployment in Windows Server](./deployment/windows/README.md)

Desired showcases:

* Kubernetes
* AWS ECS / Fargate with AWS managed services (e.g. Aurora, OpenSearch, Kafka)
* Azure Container Services with Azure managed services (e.g. Cosmos, ElasticSearch, Kafka)
* Google Cloud Run (e.g. ... similar)

## Plugins

The DMS source code currently has three core interfaces for communicating with
the outside world. Although not currently (3/30/2025) configured as such, the
intention is to utilize the [.NET plugin architecture](../docs/PLUGIN.md) to
support dropping new plugins for these interfaces in at runtime, so that they do
not need to be compiled with the core application.

1. Frontend: currently only support ASP.NET, could also have cloud function
   front ends.
2. Persistence: currently only supporting PostgreSQL, could use any database
   that fits the system requirements. This interface covers the following
   operations: create, update, delete, and get by ID. The get by ID function is
   here so that clients who have just written a record can immediately retrieve
   that record without any eventual consistency concerns.
3. Query: currently supporting both OpenSearch and Elasticsearch,
   could use any database system. This system should be tuned for high
   performance indexed-queries.

In addition, the DMS Platform supports client authentication and authorization
via OpenID Connect (OIDC), which is an extension of OAuth 2. The DMS itself does
not need any special code (in theory) for integrating with different OIDC
providers - that is the point of the OIDC standard. However, neither OAuth 2 nor
OIDC specify how to _create and manage_ client credentials. Each Identity
Provider (IdP) has its own way of handling this.

In the DMS Platform, the DMS Configuration Service takes responsibility for
creating client credentials that have the claims used by the DMS for
authorization, including _role_ and _scopes_. The DMS Configuration Service also
needs to store metadata such as the definition of an authorization claimset, and
provides a hierarchy on top of client credentials (Vendor > Application >
Credentials). These metadata must be persisted in their own backend data store.
Thus the DMS Configuration Service has two key interfaces amenable to plugin
development:

1. Identity provider
2. Persistence

Showcases:

* [Towards a flattened MSSQL Backend](./plugins/flattened-mssql/README.md)

Desired showcases:

* Integrating OpenID Dict into the Configuration Service for self-hosted
  authorization, as an alternative to running an external IdP. (Similar to the
  Ed-Fi Admin API application).
* Building a compatibility layer for AWS Cognito or other cloud-based IdPs.

## Streaming

The single-table design of the PostgreSQL write backend system simplifies
integration into a streaming platform: there is only one table to monitor, and
its records contain the complete JSON document for resources. These documents
are aligned to the Ed-Fi Data Standard. The out-of-the-box use case for
streaming is to create a high-performance, real-time ETL process that loads data
into the search database (OpenSearch or Elasticsearch).

Showcases:

* [Proton Streaming Database for Analytics](./streaming/proton/README.md)
* [Realtime ETL with Rising Wave](./streaming/rising-wave/README.md)
* [Streaming to S3 Object Storage](./streaming/S3-storage/README.md)

Desired showcases:

* Capturing and analyzing logs.
* ELT / ETL
* Use of materialized views either saved back into a Kafka topic or in a
  stateful stream processor.
* Health monitoring (document counts).
  * Question: do we have enough information to distinguish inserts and updates?
    Should be incrementing on POST only if a real insert, not an upsert. Should
    decrement on delete.
  * Displaying data with [Grafana](https://github.com/timeplus-io/proton-grafana-source).
* Re-publishing data to a downstream ODS/API or DMS installation, e.g. for state
  / local synchronization.
  * Does this eliminate the need to setup API Publisher?
  * What about the Change Queries API?
* Real-time [level 2
  validation](https://docs.ed-fi.org/getting-started/sea-playbook/project-planning/embracing-data-validation-with-the-ed-fi-odsapi/)
  with results posted into another system, for example storing into the
  [DataChecker database](https://github.com/Ed-Fi-Exchange-OSS/DataChecker) or
  publishing results into a [Validations API
  server](https://edfi.atlassian.net/wiki/spaces/ESIG/pages/25495883/Ed-Fi+Validation+API+Design).
  Or even publishing back to Kafka into a validation event stream.
* Calculations - for example, realtime attendance rate by student.
  * Note: would need to emphasize that the algorithm for attendance rate can
    differ, for example, which descriptors count as absent?
  * Take corrections into account.
* Notifications, perhaps paired with attendance rate calculations.

Possible stream processing tools:

* Libraries
  * [Kafka Streams](https://kafka.apache.org/40/documentation/streams/) - Java
  * Quix-Streams
    * [Python](https://github.com/quixio/quix-streams)
    * [.NET](https://github.com/quixio/quix-streams-dotnet)
  * [no-kafka](https://github.com/oleksiyk/kafka) - JavaScript
* Streaming Databases
  * Confluent [ksqlDB](https://github.com/confluentinc/ksql)
  * Proton
    * [Java](https://github.com/timeplus-io/proton-java-driver)
    * [Python](https://github.com/timeplus-io/proton-python-driver)
    * [Go](https://github.com/timeplus-io/proton-go-driver)
    * [ODBC](https://github.com/timeplus-io/proton-odbc) - theoretically useful for .NET, JavaScript
* Stream Processors
  * [Apache Flink](https://flink.apache.org/)
  * [Apache Spark](https://spark.apache.org/docs/latest/structured-streaming-kafka-integration.html) - Python, Java, Scala

## AI

What can we do to make the DMS more useful for machine learning processes? For
integration into large language models (LLM)?

Desired showcases:

* Using Model Context Protocol (MCP) as an intermediary between an LLM and either the
  API (for fine-grained authorization) or OpenSearch (when all data should be
  available, or with custom RBAC).

> [!WARNING]
> MCP is an interesting idea, but is very new and may not be very securable
> yet. Use with extreme caution.

* Using OpenSearch's [AI
  Search](https://opensearch.org/docs/latest/vector-search/ai-search/index/) to
  convert indexes to vectors, and incorporating into a local model.

## Search Database

The search database (OpenSearch or Elasticsearch) supports "get all" and "get by
query" workloads in the DMS. What else can these databases do?

Showcases:

* [Simple Dashboards in OpenSearch](./search/simple-dashboards/README.md)

Desired showcases:

* Log ingestion and analysis from DMS and/or other components
