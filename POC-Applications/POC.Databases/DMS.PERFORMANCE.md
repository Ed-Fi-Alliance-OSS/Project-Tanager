# Database matrices

This document offers matrices for database tables, providing a detailed breakdown of each table's structure
as well as performance insights across various database operations.
Additionally, it offers details of partitioning and clustered indexes within the database.

## DataTable Name: [dbo].[Documents]

| Column Name            | Data Type        | Description |
|------------------------|------------------|-----------------|
| id                     | BIGINT           | Identity column |
| document_partition_key | TINYINT          | Partition key for this table, derived from document_uuid |
| document_uuid          | UNIQUEIDENTIFIER | API resource id, clustered |
| resource_name          | TINYINT          | Example: Student |
| edfi_doc               | VARBINARY(MAX)   | The document |

### Indexes
  Clustered Index Columns: document_partition_key, id <br>
  NonClustered Index Columns: document_partition_key, document_uuid

### Partition
  Strategy: List partitioning  <br>
  Scheme Name: partition_scheme_Documents  <br>
  Function Name: partition_function_Documents

## DataTable Name: [dbo].[Aliases]

| Column Name                 | Data Type        | Description |
|-----------------------------|------------------|-----------------|
| id                          | BIGINT           | Identity column |
| referential_partition_key   | TINYINT          | Partition key for this table, derived from referential_id |
| referential_id              | UNIQUEIDENTIFIER | Extracted or superclassed document identity |
| document_id                 | BIGINT           | Actual document id |
| document_partition_key      | TINYINT          | Actual document partition key |

### Indexes
  Clustered Index Columns: partition_key, id <br>
  NonClustered Index Columns: referential_partition_key, referential_id

### Partition
  Strategy: List partitioning  <br>
  Scheme Name: partition_scheme_Aliases  <br>
  Function Name: partition_function_Aliases  <br>

## DataTable Name: [dbo].[References]

| Column Name               | Data Type | Description |
|---------------------------|-----------|-----------------|
| id                        | BIGINT    | Identity column |
| document_id               | BIGINT    | Document id of parent document, non-unique non-clustered partition-aligned |
| document_partition_key    | TINYINT   | Partition key, same as Documents.document_partition_key of parent document |
| referenced_alias_id       | BIGINT    | Alias of document being referenced |
| referenced_partition_key  | BIGINT    | Partition key of Aliases table, derived from Aliases.referential_id |

### Indexes
  Clustered Index Columns: document_partition_key, id <br>
  NonClustered Index Columns: document_partition_key,  document_id

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
    Insert ( The insert operation will involve inserting a new record into table `Documents`. Additionally,
    it will create an alias record in table `Aliases` and generate references entries in table `References`.)

    Note: Each insertion into the Documents table results in the creation of five references records inserted
    into the References table.

#### 10,000 documents

| Table Name                | OperationName | Total Records  | ExecutionTime | Data Space Used (KB) | Index Space Used (KB) |
|---------------------------|---------------|----------------|---------------|----------------------|-----------------------|
| Student                   | INSERT        | 70015          | 0:02:07       | 4976                 | 256                   |
| StudentSchoolAssociation  | INSERT        | 70015          | 0:02:09       | 8480                 | 256                   |
| StudentSectionAssociation | INSERT        | 70015          | 0:02:10       | 8510                 | 256                   |

#### 100,000 documents

| Table Name                | OperationName | Total Records  | ExecutionTime | Data Space Used (KB) | Index Space Used (KB) |
|---------------------------|---------------|----------------|---------------|----------------------|-----------------------|
| Student                   | INSERT        | 700015         | 0:22:57       | 47544                | 384                   |
| StudentSchoolAssociation  | INSERT        | 700015         | 0:23:01       | 72472                | 384                   |
| StudentSectionAssociation | INSERT        | 700015         | 0:23:02       | 72490                | 384                   |

#### 1,000,000 documents

| Table Name                | OperationName | Total Records  | ExecutionTime | Data Space Used (KB) | Index Space Used (KB) | With new Indexes (KB) |
|---------------------------|---------------|----------------|---------------|----------------------|-----------------------|-----------------------|
| Student                   | INSERT        | 7000015        | 3:53:59       | 473016               | 512                   | 43552                 |
| StudentSchoolAssociation  | INSERT        | 7000015        | 3:54:01       | 714496               | 512                   | 43552                 |
| StudentSectionAssociation | INSERT        | 7000015        | 3:54:03       | 714511               | 512                   | 43552                 |

VM Improvements:
 *  Size: Standard D8as V4
 *  Windows Server 2020
 *	Microsoft SQL Server 2022
 *	32GB RAM
 *	8 vCPUs

After making some improvements to the VM, the results of running the test with 1,000,000 records are as follow:

| Table Name                | OperationName | Total Records  | ExecutionTime | Data Space Used (KB) | With new Indexes (KB) |
|---------------------------|---------------|----------------|---------------|----------------------|-----------------------|
| StudentSchoolAssociation  | INSERT        | 7000030        | 3:05:24       | 435048               | 44104                 |

With VARBINARY

| Table Name                | OperationName | Total Records  | ExecutionTime | Data Space Used (KB) | With new Indexes (KB) |
|---------------------------|---------------|----------------|---------------|----------------------|-----------------------|
| StudentSchoolAssociation  | INSERT        | 10,000         | 00:02:15      | 4632                 | 1024                  |
| StudentSchoolAssociation  | INSERT        | 100,000        | 00:22:04      | 43744                | 4632                  |
| StudentSchoolAssociation  | INSERT        | 1,000,000      | 03:13:02      | 435048               | 44104                 |

With NVARCHAR

| Table Name                | OperationName | Total Records  | ExecutionTime | Data Space Used (KB) | With new Indexes (KB) |
|---------------------------|---------------|----------------|---------------|----------------------|-----------------------|
| StudentSchoolAssociation  | INSERT        | 10,000         | 00:01:40      | 5800                 | 1032                  |
| StudentSchoolAssociation  | INSERT        | 100,000        | 00:17:04      | 57232                | 4728                  |
| StudentSchoolAssociation  | INSERT        | 1,000,000      | 02:49:42      | 571528               | 46360                 |

### Select Query Details

    Select with guid
    SELECT * FROM [EdFi_DataManagementService].[dbo].[Documents] where document_uuid = 'D14CBB83-81F5-48FC-BB49-F23E4359D6E0'

    Select with guid and partition key:
    SELECT * FROM [EdFi_DataManagementService].[dbo].[Documents]
    where document_uuid = 'D14CBB83-81F5-48FC-BB49-F23E4359D6E0' AND partition_key = 0


| Table Name   | OperationName | ExecutionTime | Number Of Rows affected  | Details                                               |
|--------------|---------------|---------------|--------------------------|-------------------------------------------------------|
| Documents    | SELECT        | 0:00:42       | 1 from 1 mill records    | Where condition with document_uuid                    |
| Documents    | SELECT        | 0:00:07       | 1 from 1 mill records    | Where condition with document_uuid and partition_key  |
| Documents    | UPDATE        | 0:00:19       | 1000                     |                                                       |
| Documents    | INSERT        | 0:00:15       | 10000                    | With 5 references per document and query table insert |
| References   | SELECT        | 0:00:34       | 10000000                 | Select * from References                              |
| References   | DELETE        | 0:00:09       | 10000000                 | Delete from References                                |
| Documents    | INSERT        | 0:00:41       | 30000                    | With 5 references per document and query table insert |


