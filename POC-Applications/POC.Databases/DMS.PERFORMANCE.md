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
Data Operations

### Insert
    Insert ( The insert operation will involve inserting a new record into table `Documents`. Additionally, it will create an alias record in table `Aliases` and generate references entries in table `References`.)

### Select Query Details

    Select with guid
    SELECT1: SELECT * FROM [EdFi_DataManagementService].[dbo].[Documents] where document_uuid = 'D14CBB83-81F5-48FC-BB49-F23E4359D6E0'

    Select with guid and partition key:
    SELECT2: SELECT * FROM [EdFi_DataManagementService].[dbo].[Documents]
    where document_uuid = 'D14CBB83-81F5-48FC-BB49-F23E4359D6E0' AND partition_key = 0

| Table Name   | OperationName | ExecutionTimeInSeconds | Number Of Rows affected  | Details                         |
|--------------|---------------|------------------------|--------------------------|---------------------------------|
| Documents    | INSERT        | 70                     | 100000                   | With 1 reference per document   |
| Documents    | INSERT        | 1994                   | 1000020                  | With 10 references per document |
| Documents    | SELECT1       | 42                     | 1                        |                                 |
| Documents    | SELECT2       | 7                      | 1                        |                                 |
| Documents    | UPDATE        | 19                     | 1000                     |                                 |


| Table Name | Number Of Rows | Reserved Size(KB) |Index Size(KB) | Data Size(KB) | Unused Size(KB) |
|------------|----------------|-------------------|---------------|---------------|-----------------|
| Documents  | 1000020        | 716864            | 512           | 714496        | 1856            |
| References | 10000000       | 360384            | 1920          | 357208        | 1256            |
| Aliases    | 1000040        | 44160             | 256           | 42616         | 1288            |

