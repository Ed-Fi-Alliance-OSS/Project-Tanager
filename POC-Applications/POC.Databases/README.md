# Database matrices

This document offers matrices for database tables, providing a detailed breakdown of each table's structure
as well as performance insights across various database operations.
Additionally, it offers details of partitioning and clustered indexes within the database.

## DataTable Name: [dbo].[Documents]

| Column Name   | Data Type        | Description |
|---------------|------------------|-----------------|
| id            | BIGINT           | Identity column |
| partition_key | TINYINT          | Partition key for this table, derived from document_uuid |
| document_uuid | UNIQUEIDENTIFIER | API resource id, clustered |
| resource_name | TINYINT          | Example: Student |
| edfi_doc      | VARBINARY(MAX)   | The document |

### Indexes:
  Clustered Index:
  Columns: partition_key, id

### Partition
  Strategy: List partitioning
  Scheme Name: partition_scheme_Documents
  Function Name: partition_function_Documents

## DataTable Name: [dbo].[Aliases]

| Column Name                 | Data Type        | Description |
|-----------------------------|------------------|-----------------|
| id                          | BIGINT           | Identity column |
| partition_key               | TINYINT          | Partition key for this table, derived from document_uuid |
| referential_id              | UNIQUEIDENTIFIER | Extracted or superclassed document identity |
| document_id                 | BIGINT           | Actual document id |
| document_partition_key      | TINYINT          | Actual document partition key |

### Indexes:
  Clustered Index:
  Columns: partition_key, id

### Partition
  Strategy: List partitioning
  Scheme Name: partition_scheme_Aliases
  Function Name: partition_function_Aliases

## DataTable Name: [dbo].[References]

| Column Name               | Data Type | Description |
|---------------------------|-----------|-----------------|
| id                        | BIGINT    | Identity column |
| partition_key             | TINYINT   | Partition key for this table, derived from parent_referential_id |
| parent_alias_id           | BIGINT    | API resource id, clustered |
| parent_partition_key      | TINYINT   | Example: Student |
| referenced_alias_id       | BIGINT    | The document |
| referenced_partition_key  | BIGINT    | The document |

### Indexes:
  Clustered Index:
  Columns: partition_key, id

### Partition
  Strategy: List partitioning
  Scheme Name: partition_scheme_References
  Function Name: partition_function_References

## Performance Considerations:
Data Volume: 100000

Data Operation: Insert ( The insert operation will involve inserting a new record into table `Documents`. Additionally, it will create an alias record in table `Aliases` and generate reference entries in table `References`.)

Performance Metrics:
    Execution Time: 1.10 minutes

