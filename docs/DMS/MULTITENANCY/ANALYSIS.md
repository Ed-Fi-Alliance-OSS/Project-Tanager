# Introduction

Adding support for multitenant deployments introduces new privacy concerns as it pertains to physical storage of sensitive student data that is subject to FERPA regulations. The goal of this analysis was to identify and document the options available for handling multitenant data storage.

# Data Segregation Analysis

The current DMS solution consists of the following architectural components that physically store sensitive data:

* PostgreSQL databases for primary data storage
* Kafka storage for streaming data
* OpenSearch for serving GET requests from the API.

Each of these components have distinct characteristics and approaches that would be needed to achieve various levels of logical and physical data segregation between tenants.

ℹ️ In many discussions about multitenancy, the term tenant is used to define the boundaries for data segregation. However, in the Ed-Fi API solutions, a tenant refers to the scope of administrative control for managing multiple ODS/DMS _instances_. Each API client is associated with a specific ODS/DMS instance and should not be able to see data from other ODS/DMS instances, even though they may be provisioned by the same tenant. Thus, the focus of this document is around data segregation at the _instance_ level.

## Database (PostgreSQL or SQL Server)

With relational databases, the data segregation options are well documented and widely understood due to the maturity of the technology. The primary options are as follows:

### Row-level data segregation (low isolation)

Every table in the database would have an "instance id" column that is then used by the application to filter data for each request. Alternatively, the database engine may support row-level security but this would have other implication on the connection management and pooling that probably make it impractical. It is only being mentioned here for completeness because while it is technically possible, it is deemed to present higher risk for the Ed-Fi Alliance given privacy concerns around student data and FERPA regulations.

### Schema-level data segregation (medium isolation)

Every instance would have a separate _schema_ in a shared DMS database. While serving to reduce the number of databases needed to host multiple instances and thereby reducing costs for hosting models with an incremental per-database cost, it would come with extra management and support complexity as compared with row-level or database-level approaches. Schema-based segregation does offer less risk of data exposure than row-level filtering as an "unfiltered" query is likely to fail due to an invalid schema rather than expose other instances' data, but in practice it is not seen as a scalable or supportable strategy. For these reasons it is mentioned here only for completeness rather than as a recommended option.

### Database-level segregation (high isolation)

Every instance would have its own database. Databases can be created and deleted easily per tenant instance, providing the flexibility needed for horizontal scalability. This also provides the highest level of data segregation but could result in higher costs if the hosting model has an associated per-database cost.

### Hybrid Approach

It is worth noting that these three approaches are not mutually exclusive. For example, one could use both schema-level or row-level segregation along with database-level segregation to manage a large implementation. A very large school district would likely be allocated a dedicated database, while many very small districts could be combined into a single database.

The API software _could_ support such flexible deployment and management options with a design that incorporates the following items:

* Each API client is associated with a specific instance.
* Each instance has an associated configured connection string to a specific DMS database.
* The API implementation incorporates instance-based schemas into all queries as appropriate for the API client while processing requests.

## Kafka

Kafka plays a central role in the DMS architecture as a durable, high-throughput backbone for streaming data between components. However, Kafka was not originally designed for strict multitenancy or per-tenant isolation and supporting high levels of instance-level data segregation in Kafka requires careful consideration. The following approaches were evaluated.

### Cluster-per-instance segregation (least feasible)

Running a dedicated Kafka cluster per instance would theoretically provide complete physical isolation of data streams. However, this is entirely impractical for a large scale (e.g. 1300 instances for Texas). Kafka clusters are complex to deploy and manage, and the operational overhead of managing thousands of brokers, Zookeeper (or KRaft) nodes, and associated monitoring infrastructure seems to be quite impractical. This approach is therefore excluded from serious consideration.

### Topic-per-instance segregation (current approach; feasible within limits)

This approach offers strong isolation between instances and aligns well with data separation requirements related to FERPA. It simplifies ACL configuration (for controlling which consumers can access the data), consumer group management, and auditing, since each instance's data is fully separated at the topic level. This provides clear instance-based isolation at the Change Data Capture (CDC) level for 3rd party consumers.

However, for use with OpenSearch in such a configuration, it would probably make sense to route the instance-specific topics into a single consolidated topic for ingestion into a solution-level service such as OpenSearch. It would also be necessary to perform some additional transformations along the way to retain the tenant/instance identification for use in OpenSearch indexes to provide the necessary logical segregation of the data for API clients (e.g. via filtered aliases).

### Shared topic with tenant/instance filtering (technically possible, but least desirable)

An alternative model would consolidate messages for all instances into a single shared (i.e. massive) topic and rely on an instance identifier field in the message payload to filter and route messages. While this reduces topic count and centralizes stream processing, it increases the risk of cross-instance data leakage due to misconfigured consumers or processing bugs.

The real problems emerge when that single, massive topic is consumed by various downstream applications, including sink connectors. If all of the instances' data is in one topic and every instance-based consumer is interested in (or more aptly, allowed access to) only a small subset of that data, they will each have to read **all** of the data and filter out a majority of it. This results in wasted CPU cycles, network bandwidth, and increased latency for processing. These characteristics may also limit the effectiveness of topic partitioning for scalability using consumer groups.

As discussed above, it might make sense to combine all data into a single topic for ingestion to a shared OpenSearch instance but for an architecture striving to provide streaming consumption by 3rd party consumers with instance-based access, this is not ideal. Given the sensitivity of student data and the FERPA compliance goal of strong per-instance segregation, an approach based exclusively on a shared topic is not recommended.

## Kafka Connect

The choice of relational database engine (PostgreSQL or SQL Server) and the database multitenancy strategy (database/schema/row-level segregation) both have a strong influence on how Debezium and Kafka Connect must be configured to accurately capture, route, and sink tenant/instance-specific data into OpenSearch.

For PostgreSQL, the Debezium connector uses a replication slot that is scoped to a single database. This means it cannot span multiple databases. It's important to understand that connectors in Kafka Connect are logical configurations—they do not correspond to operating system processes or heavy services. Each connector is broken down into tasks that are distributed across the Kafka Connect cluster workers.

In contrast, Debezium's integration with SQL Server does not have the same connector-per-database limitation. The connector relies on CDC tables created within each SQL Server database and can be configured to process changes from multiple databases on the same server. Therefore, the minimum number of connectors required is tied to the number of SQL Server instances being monitored, not the number of databases.

Operationally, the main concern is not the number of connectors but the management of the Kafka Connect cluster itself. Factors such as worker capacity, scaling, and fault tolerance are more critical. While strategies like database-per-instance or schema-per-instance can influence connector counts, they should not be overstated as the primary factor in overall manageability. The real scaling challenges are at the Kafka Connect cluster level.

## OpenSearch

OpenSearch presents unique challenges for supporting multitenancy at the scale required by a large SEA (e.g. Texas), particularly when considering the current implementation pattern. The following data segregation strategies are evaluated in order from least feasible to most feasible.

### Cluster-level segregation (high isolation, least feasible)

At the highest level of physical isolation, a dedicated OpenSearch cluster-per-instance approach would ensure that each tenant's data is fully isolated in terms of both storage and compute. However, this approach is prohibitively expensive and operationally complex. Managing and maintaining 1300+ clusters would place an unsustainable burden on the hosting organization. This model also presents challenges for resource utilization, with a very high likelihood of underutilization across many clusters. As such, this option is not considered feasible for the intended scale and support model.

### Index-per-instance segregation (very problematic based on current indexing scheme)

The current implementation uses an index-per-resource strategy, resulting in approximately 385 indexes. Extrapolating this strategy to a deployment supporting 1300 instances would result in about half a million indexes. This creates significant issues:

* OpenSearch maintains cluster state metadata in memory, and the cost of tracking large numbers of indexes and shards can degrade cluster performance and stability.
* Operations such as snapshotting, cluster restarts, and mappings updates scale poorly as the number of indexes increases.
* This approach represents **an architectural anti-pattern** in the OpenSearch and Elasticsearch ecosystem for large-scale multitenancy, as it does not scale efficiently and leads to frequent operational issues.

ℹ️ See [OpenSearch Cluster State](https://opster.com/guides/opensearch/opensearch-capacity-planning/opensearch-cluster-state/) and [Multi-Tenancy with Elasticsearch and OpenSearch](https://bigdataboutique.com/blog/multi-tenancy-with-elasticsearch-and-opensearch-c1047b).

Given these concerns, the index-per-tenant approach is likely not feasible even if the current indexing strategy gets a fundamental redesign to meet long-term scalability and reliability goals.

### Shared indexes with filtered aliases (low isolation, most feasible)

The data for each instance would be accessed by the API through _filtered aliases_ in OpenSearch, which would enforce instance-level access constraints using query-time filtering on an instance identifier field.

This approach prevents further exacerbation of the "index explosion" problem presented by an index-per-resource-per-instance strategy, avoiding degradation of overall performance and stability of the cluster as it scales. However, the approach requires careful implementation with consideration to FERPA requirements as follows:

* All API requests must be executed in the context of an alias with an appropriate filter applied to ensure instance-level data access boundaries. Failure to use the aliases correctly and pervasively in the application code could expose the wrong data to API consumers.
* Care must be taken with data lifecycle policies, re-indexing, and versioning to ensure tenant-specific requirements can still be met within shared indexes.

While this approach does introduce additional complexity in terms of query formulation and alias management, it provides the most viable path forward for supporting multitenancy in OpenSearch at the required scale, balancing performance, manageability, and FERPA-compliant data isolation.

### Concerns Regarding Index-Per-Resource Approach

The current DMS implementation creates an index for each resource. The current Ed-Fi model has 200+ descriptors and this results in _many_ very small indexes, which is an anti-pattern. Each index requires its own shard and adds to the cluster state management. Additionally, it is recommended that indexes should be between 10GB and 50GB in size and the resulting descriptor indexes are incredibly small and unlikely to grow much even in large scale multitenant production deployments.

A more scalable and operationally efficient solution would involve consolidating data into a smaller number of shared indexes, possibly one per resource type (e.g. descriptors), per domain, or based on anticipated resource size (e.g. grouping smaller resources together while keeping larger resources separate).

Consideration may also be given to using a single shared index as there are only about 500-600 unique _column_ names in the current Ed-Fi ODS database and OpenSearch defines a default maximum of 1000 fields per index. However, this number doesn't necessarily translate directly to the number of fields present in the JSON bodies presented to OpenSearch for indexing. For example, EducationOrganizationId could appear at the root level or in differently named references across various resources.

There are also strategies that could be used to reduce the number of fields through explicit model metadata-driven mapping generation. It is noteworthy that the current API specification provides no support for filtering on values of child objects in resources and so the current OpenSearch connector is likely over-indexing by including many fields that will never be used by the API.

When combined, these approaches would significantly reduce the number of indexes and shards managed by the cluster and reduce the number of fields in the indexes, likely improving overall performance and stability of the cluster as it scales. Of course, all of this optimization comes at a cost – additional complexity.

# Design Direction and Next Steps

Here are the next steps based on the analysis above:

* Database (Hybrid Approach)
  * Implement support for multiple instances through multiple DMS databases.
  * Replicate the behavior of the Ed-Fi ODS API Admin metadata related to associating each API client with a specific instance and use the instance-specific connection string to connect to the appropriate DMS database.
* Debezium/Kafka (Topic-per-Instance Segregation)
  * Publish changes into instance-based topics to make it more feasible to implement appropriate authorization guardrails for 3rd party Kafka consumers.

ℹ️ After analysis and evaluation, plans for utilizing OpenSearch/ElasticSearch to support the API's read load are being dropped. They cannot provide the required support for deep offset-based pagination, which is an anti-pattern for modern search engines. Separately, replacement of the current index-per-resource approach would require considerable work including model-informed index generation. Support for Kafka will remain as an optional feature of the DMS specifically for hosts with consumers with use cases that would benefit from streaming data.
