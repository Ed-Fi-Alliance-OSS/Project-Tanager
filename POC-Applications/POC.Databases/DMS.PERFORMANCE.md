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

### Indexes
  Clustered Index Columns: partition_key, id

### Partition
  Strategy: List partitioning  <br>
  Scheme Name: partition_scheme_Documents  <br>
  Function Name: partition_function_Documents

## DataTable Name: [dbo].[Aliases]

| Column Name                 | Data Type        | Description |
|-----------------------------|------------------|-----------------|
| id                          | BIGINT           | Identity column |
| partition_key               | TINYINT          | Partition key for this table, derived from document_uuid |
| referential_id              | UNIQUEIDENTIFIER | Extracted or superclassed document identity |
| document_id                 | BIGINT           | Actual document id |
| document_partition_key      | TINYINT          | Actual document partition key |

### Indexes
  Clustered Index Columns: partition_key, id

### Partition
  Strategy: List partitioning  <br>
  Scheme Name: partition_scheme_Aliases  <br>
  Function Name: partition_function_Aliases  <br>

## DataTable Name: [dbo].[References]

| Column Name               | Data Type | Description |
|---------------------------|-----------|-----------------|
| id                        | BIGINT    | Identity column |
| partition_key             | TINYINT   | Partition key for this table, derived from parent_referential_id |
| parent_alias_id           | BIGINT    | API resource id, clustered |
| parent_partition_key      | TINYINT   | Example: Student |
| referenced_alias_id       | BIGINT    | The document |
| referenced_partition_key  | BIGINT    | The document |

### Indexes
  Clustered Index Columns: partition_key, id

### Partition
  Strategy: List partitioning  <br>
  Scheme Name: partition_scheme_References  <br>
  Function Name: partition_function_References

## Performance Considerations

### Test Environment
* Windows Server 2020
* Microsoft SQL Server 2022
* 32GB RAM
* 4 CPU 2.56 GHz

### Data Operations

### Insert
    Insert ( The insert operation will involve inserting a new record into table `Documents`. Additionally, it will create an alias record in table `Aliases` and generate references entries in table `References`.)

    Note: Each insertion into the Documents table results in the creation of five references records inserted into the References table.

#### 10,000 documents

| Table Name                | OperationName | ExecutionTime | Data Space Used (KB) | Index Space Used (KB) |
|---------------------------|---------------|---------------|----------------------|-----------------------|
| Student                   | INSERT        | 0:02:07       | 4976                 | 256                   |
| StudentSchoolAssociation  | INSERT        | 0:02:09       | 8480                 | 256                   |
| StudentSectionAssociation | INSERT        | 0:02:10       | 8510                 | 256                   |

#### 100,000 documents

| Table Name                | OperationName | ExecutionTime | Data Space Used (KB) | Index Space Used (KB) |
|---------------------------|---------------|---------------|----------------------|-----------------------|
| Student                   | INSERT        | 0:22:57       | 47544                | 384                   |
| StudentSchoolAssociation  | INSERT        | 0:23:01       | 72472                | 384                   |
| StudentSectionAssociation | INSERT        | 0:23:02       | 72490                | 384                   |

#### 1,000,000 documents

| Table Name                | OperationName | ExecutionTime | Data Space Used (KB) | Index Space Used (KB) | With new Indexes (KB) |
|---------------------------|---------------|---------------|----------------------|-----------------------|-----------------------|
| Student                   | INSERT        | 3:53:59       | 473016               | 512                   | 43552                 |
| StudentSchoolAssociation  | INSERT        | 3:54:01       | 714496               | 512                   | 43552                 |
| StudentSectionAssociation | INSERT        | 3:54:03       | 714511               | 512                   | 43552                 |

### Select Query Details

    Select with guid
    SELECT * FROM [EdFi_DataManagementService].[dbo].[Documents] where document_uuid = 'D14CBB83-81F5-48FC-BB49-F23E4359D6E0'

    Select with guid and partition key:
    SELECT * FROM [EdFi_DataManagementService].[dbo].[Documents]
    where document_uuid = 'D14CBB83-81F5-48FC-BB49-F23E4359D6E0' AND partition_key = 0


| Table Name   | OperationName | ExecutionTime | Number Of Rows affected  | Details                                               |
|--------------|---------------|---------------|--------------------------|-------------------------------------------------------|
| Documents    | SELECT        | 0:00:42       | 1                        | Where condition with document_uuid                    |
| Documents    | SELECT        | 0:00:07       | 1                        | Where condition with document_uuid and partition_key  |
| Documents    | UPDATE        | 0:00:19       | 1000                     |                                                       |
| Documents    | INSERT        | 0:00:15       | 10000                    | With 5 references per document and query table insert |
| References   | SELECT        | 0:00:34       | 10000000                 | Select * from References                              |
| References   | DELETE        | 0:00:09       | 10000000                 | Delete from References                                |
| Documents    | INSERT        | 0:00:41       | 30000                    | With 5 references per document and query table insert |

